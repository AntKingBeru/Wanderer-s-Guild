// Drives the loop's back half: forms parties for posted quests, launches on approval, resolves outcomes.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-65)]
public class QuestLifecycleController : MonoSingleton<QuestLifecycleController>
{
    [Tooltip("Fixed RNG seed (0 = time-based random).")]
    [SerializeField] private int seed;
    [Tooltip("In-game days a quest takes to resolve once launched.")]
    [SerializeField] private int questDurationDays = 3;

    private System.Random _rng;
    private int _nextPartyId = 1;
    
    private readonly Dictionary<int, Party> _parties = new Dictionary<int, Party>();
    private readonly Dictionary<int, int> _partyToQuest = new Dictionary<int, int>();
    private readonly Dictionary<int, GameDate> _questResolveOn = new Dictionary<int, GameDate>();
    private readonly Dictionary<int, int> _questToParty = new Dictionary<int, int>();

    protected override void OnSingletonAwake()
        => _rng = seed != 0 ? new System.Random(seed) : new System.Random();

    private void OnEnable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onQuestPosted.AddListener(HandleQuestPosted);
        relay.onApplicationApproved.AddListener(HandleApplicationApproved);
        relay.onDayAdvanced.AddListener(HandleDayAdvanced);
    }

    private void OnDisable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onQuestPosted.RemoveListener(HandleQuestPosted);
        relay.onApplicationApproved.RemoveListener(HandleApplicationApproved);
        relay.onDayAdvanced.RemoveListener(HandleDayAdvanced);
    }
    
    private void HandleQuestPosted(int questId)
        => TryFormAndApply(questId);
    
    private void HandleDayAdvanced(GameDate today)
    {
        RetryUnfilledQuests();
        ResolveDueQuests(today);
    }
    
    private void RetryUnfilledQuests()
    {
        if (!QuestBoard.Exists)
            return;
        for (var i = 0; i < QuestBoard.Instance.SlotCount; i++)
        {
            var q = QuestBoard.Instance.GetSlot(i);
            if (q is { State: QuestState.Posted } && !_questToParty.ContainsKey(q.Id))
                TryFormAndApply(q.Id);
        }
    }
    
    private void TryFormAndApply(int questId)
    {
        if (_questToParty.ContainsKey(questId))
            return;
        var quest = FindPostedQuest(questId);
        if (quest == null)
            return;

        var members = PartyFormationService.TryFormFor(quest, AdventurerRoster.Instance.GetAll());
        if (members == null)
            return;

        var party = new Party(_nextPartyId++, members);
        party.SetState(PartyState.Forming);
        _parties.Add(party.Id, party);

        foreach (var id in members)
            AdventurerRoster.Instance.Get(id)?.SetState(AdventurerState.Applying);

        _questToParty[questId] = party.Id;
        _partyToQuest[party.Id] = questId;

        GameEventsRelay.Instance.RaisePartyFormed(party.Id);
        ApplicationBoard.Instance.Submit(_partyToQuest[party.Id], party.Id);
    }
    
    private void HandleApplicationApproved(int applicationId)
    {
        var (questId, partyId) = ApplicationBoard.Instance.GetTargets(applicationId);
        if (questId < 0 || !_parties.TryGetValue(partyId, out var party))
            return;

        var quest = FindPostedQuest(questId);
        if (quest == null)
            return;
        if (!quest.TrySetState(QuestState.InProgress))
            return;

        party.SetState(PartyState.OnQuest);
        foreach (var id in party.MemberIds)
            AdventurerRoster.Instance.Get(id)?.SetState(AdventurerState.OnQuest);

        var resolveOn = TimeController.Instance.CurrentDate.AddDays(
            Mathf.Max(1, questDurationDays), GameConfig.Instance.Time.daysPerSeason);
        _questResolveOn[questId] = resolveOn;

        var relay = GameEventsRelay.Instance;
        relay.RaiseQuestStateChanged(questId, QuestState.InProgress);
        relay.RaiseApplicationReceived(applicationId);
        ApplicationBoard.Instance.Remove(applicationId);
    }
    
    private void ResolveDueQuests(GameDate today)
    {
        var due = (from kvp in _questResolveOn where today.CompareTo(kvp.Value) >= 0 select kvp.Key).ToList();

        foreach (var questId in due)
            ResolveQuest(questId);
    }
    
    private void ResolveQuest(int questId)
    {
        _questResolveOn.Remove(questId);
        if (!_questToParty.TryGetValue(questId, out var partyId) ||
            !_parties.TryGetValue(partyId, out var party))
            return;

        Quest quest = FindAnyQuest(questId);
        if (quest == null)
        {
            Cleanup(questId, partyId);
            return;
        }
        
        var members = new List<Adventurer>();
        var partyTotal = 0;
        foreach (var id in party.MemberIds)
        {
            var a = AdventurerRoster.Instance.Get(id);
            if (a != null)
            {
                members.Add(a);
                partyTotal += a.Stats.Total;
            }
        }

        var outcome = QuestResolver.Resolve(quest, members, _rng);
        var reputationDelta = outcome.reputationDelta;
        
        foreach (var a in members)
        {
            var died = false;
            if (!outcome.success)
                died = QuestResolver.RollCasualty(quest, partyTotal, _rng);

            if (died)
            {
                reputationDelta += GameConfig.Instance.Resolution.reputationOnDeath;
                AdventurerRoster.Instance.Remove(a.Id, DepartureReason.Death);
                continue;
            }

            if (outcome.success)
                a.AddGold(outcome.goldPerSurvivor);
            var levels = a.AddExperience(outcome.experiencePerMember);
            if (levels > 0)
                GameEventsRelay.Instance.RaiseAdventurerLeveledUp(a.Id);
            if (outcome.success)
                a.AddRankProgress(outcome.rankProgressPerMember);

            a.SetState(AdventurerState.Idle);
        }

        quest.TrySetState(outcome.success ? QuestState.Succeeded : QuestState.Failed);

        var relay = GameEventsRelay.Instance;
        relay.RaiseQuestResolved(questId, outcome.success);
        relay.RaiseQuestStateChanged(questId, outcome.success ? QuestState.Succeeded : QuestState.Failed);
        relay.RaiseQuestOutcome(questId, new QuestOutcome(
            outcome.success, outcome.goldToGuild, outcome.goldPerSurvivor,
            outcome.experiencePerMember, outcome.rankProgressPerMember, reputationDelta));
        relay.RaiseReputationChanged(reputationDelta,
            outcome.success ? ReputationChangeReason.QuestSuccess : ReputationChangeReason.QuestFailure);

        FreeQuestSlot(questId);
        Cleanup(questId, partyId);
    }
    
    private void Cleanup(int questId, int partyId)
    {
        if (_parties.TryGetValue(partyId, out var party))
            party.SetState(PartyState.Disbanding);
        _parties.Remove(partyId);
        _partyToQuest.Remove(partyId);
        _questToParty.Remove(questId);
        GameEventsRelay.Instance.RaisePartyDisbanded(partyId);
    }
    
    private void FreeQuestSlot(int questId)
    {
        if (!QuestBoard.Exists)
            return;
        for (var i = 0; i < QuestBoard.Instance.SlotCount; i++)
            if (QuestBoard.Instance.GetSlot(i)?.Id == questId)
            {
                QuestBoard.Instance.ClearSlot(i);
                return;
            }
    }

    private Quest FindPostedQuest(int questId)
        => FindAnyQuest(questId, postedOnly: true);
    
    private Quest FindAnyQuest(int questId, bool postedOnly = false)
    {
        if (!QuestBoard.Exists)
            return null;
        for (var i = 0; i < QuestBoard.Instance.SlotCount; i++)
        {
            var q = QuestBoard.Instance.GetSlot(i);
            if (q != null && q.Id == questId)
                return postedOnly && q.State != QuestState.Posted ? null : q;
        }
        return null;
    }
}