// Singleton board: holds unposted draft quests and rank-scaled posted-quest slots.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class QuestBoard : MonoSingleton<QuestBoard>
{
    private readonly Dictionary<int, Quest> _drafts = new Dictionary<int, Quest>();
    private Quest[] _slots;
    private readonly List<int> _expiryScratch = new List<int>();

    public int SlotCount => _slots?.Length ?? 0;
    public int DraftCount => _drafts.Count;

    protected override void OnSingletonAwake()
        => ResizeSlots();
    
    private void OnEnable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onDayAdvanced.AddListener(HandleDayAdvanced);
        relay.onGuildRankChanged.AddListener(HandleGuildRankChanged);
    }

    private void OnDisable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onDayAdvanced.RemoveListener(HandleDayAdvanced);
        relay.onGuildRankChanged.RemoveListener(HandleGuildRankChanged);
    }
    
    public Quest CreateFromRequest(int requestId, QuestBuilder builder, out string error)
    {
        error = null;
        if (!RequestBoard.Exists)
        {
            error = "Request board unavailable.";
            return null;
        }
        var request = RequestBoard.Instance.Get(requestId);
        if (request == null)
        {
            error = "Request no longer available.";
            return null;
        }

        if (builder == null)
        {
            error = "No quest configuration provided.";
            return null;
        }
        if (!builder.Validate(out error))
            return null;

        var now = TimeController.Instance.CurrentDate;
        var quest = builder.Build(IdService.Instance.Next(IdService.Quest), now);
        if (quest == null)
        {
            error = "Quest configuration was invalid.";
            return null;
        }

        RequestBoard.Instance.Remove(requestId);
        _drafts.Add(quest.Id, quest);

        GameEventsRelay.Instance.RaiseQuestCreated(quest.Id);
        return quest;
    }
    
    public bool PostToSlot(int questId, int slotIndex, out string error)
    {
        error = null;
        if (_slots == null || slotIndex < 0 || slotIndex >= _slots.Length)
        {
            error = "Invalid slot.";
            return false;
        }

        if (_slots[slotIndex] != null)
        {
            error = "Slot already occupied."; 
            return false;
        }

        if (!_drafts.TryGetValue(questId, out var quest))
        {
            error = "Quest is not an available draft.";
            return false;
        }

        if (!quest.TrySetState(QuestState.Posted))
        {
            error = "Quest cannot be posted.";
            return false;
        }

        _drafts.Remove(questId);
        _slots[slotIndex] = quest;

        var relay = GameEventsRelay.Instance;
        relay.RaiseQuestPosted(quest.Id);
        relay.RaiseQuestStateChanged(quest.Id, QuestState.Posted);
        return true;
    }
    
    public bool ClearSlot(int index)
    {
        if (_slots == null || index < 0 || index >= _slots.Length || _slots[index] == null)
            return false;
        _slots[index] = null;
        return true;
    }
    
    public Quest GetDraft(int id)
        => _drafts.GetValueOrDefault(id);
    
    public IReadOnlyList<Quest>
        GetDrafts() => new List<Quest>(_drafts.Values);
    
    public Quest GetSlot(int index)
        => _slots != null && index >= 0 && index < _slots.Length ? _slots[index] : null;
    
    private void ResizeSlots()
    {
        var count = GuildController.Exists
            ? GuildController.Instance.BoardSlotCount
            : GameConfig.Instance.Guild.boardSlotBase;
        if (_slots != null && _slots.Length == count)
            return;

        var resized = new Quest[count];
        if (_slots != null)
        {
            var copy = System.Math.Min(_slots.Length, count);
            for (var i = 0; i < copy; i++)
                resized[i] = _slots[i];
        }
        _slots = resized;
    }

    private void HandleGuildRankChanged(GuildRank _)
        => ResizeSlots();
    
    private void HandleDayAdvanced(GameDate today)
    {
        if (_slots == null)
            return;
        _expiryScratch.Clear();
        for (var i = 0; i < _slots.Length; i++)
        {
            var q = _slots[i];
            if (q is { State: QuestState.Posted } && q.IsExpired(today))
                _expiryScratch.Add(i);
        }

        foreach (var i in _expiryScratch)
        {
            var q = _slots[i];
            q.TrySetState(QuestState.Expired);
            _slots[i] = null;

            var relay = GameEventsRelay.Instance;
            relay.RaiseQuestStateChanged(q.Id, QuestState.Expired);
            relay.RaiseQuestExpired(q.Id);
        }
    }
}