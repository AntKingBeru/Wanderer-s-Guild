// Keeps each posted quest in the same board slot while it stays posted; new quests take the lowest free open slot.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class QuestBoardLayout
    {
        private readonly Quest[] _slots;
        private readonly Dictionary<int, int> _slotByQuest = new();
        private readonly HashSet<int> _alive = new();
        private readonly List<Quest> _byId = new();
        private bool _warnedOverflow;

        public QuestBoardLayout(int slotCount) => _slots = new Quest[Mathf.Max(1, slotCount)];

        // The quest pinned in each slot (null = free).
        public IReadOnlyList<Quest> Slots => _slots;

        // NOTE: Ended quests free their slot; kept quests stay put; new quests (in posting order) fill the lowest free
        // open slot. A quest that fits no slot (only possible with an edited save) is skipped with one warning.
        public void Update(IReadOnlyList<Quest> quests, int openSlots)
        {
            _alive.Clear();
            foreach (var quest in quests)
                _alive.Add(quest.Id);

            List<int> ended = null;
            foreach (var id in _slotByQuest.Keys.Where(id => !_alive.Contains(id)))
            {
                (ended ??= new List<int>()).Add(id);
            }
            if (ended != null)
            {
                foreach (var id in ended)
                    _slotByQuest.Remove(id);
            }

            System.Array.Clear(_slots, 0, _slots.Length);
            _byId.Clear();
            _byId.AddRange(quests);
            _byId.Sort((a, b) => a.Id.CompareTo(b.Id));

            foreach (var quest in _byId)
            {
                if (_slotByQuest.TryGetValue(quest.Id, out var slot) && slot < _slots.Length)
                    _slots[slot] = quest;
            }

            var open = Mathf.Clamp(openSlots, 0, _slots.Length);
            foreach (var quest in _byId)
            {
                if (_slotByQuest.ContainsKey(quest.Id))
                    continue;

                var free = FirstFree(open);
                if (free < 0)
                    free = FirstFree(_slots.Length); // Over the open count (edited save): use any physical slot.
                if (free < 0)
                {
                    if (!_warnedOverflow)
                    {
                        Debug.LogWarning($"[QuestBoard] {quests.Count} quests but only {_slots.Length} board slots; some aren't shown.");
                        _warnedOverflow = true;
                    }
                    continue;
                }

                _slots[free] = quest;
                _slotByQuest[quest.Id] = free;
            }
        }

        public int SlotOf(int questId) => _slotByQuest.GetValueOrDefault(questId, -1);

        private int FirstFree(int limit)
        {
            for (var i = 0; i < limit; i++)
            {
                if (_slots[i] == null)
                    return i;
            }
            return -1;
        }
    }
}