// One member's satisfaction state: fading memories (one per reason), idle dawns and Miserable dawns in a row, last level.
using System;
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: A new memory of the same reason REPLACES the old one (supper every evening refreshes, never stacks); a zero or
    // instant memory just clears that reason.
    public sealed class AdventurerMood
    {
        private readonly List<MoodMemory> _memories = new();

        public AdventurerMood(int adventurerId) => AdventurerId = adventurerId;

        public int AdventurerId { get; }
        public IReadOnlyList<MoodMemory> Memories => _memories;
        public int IdleDays { get; set; } // NOTE: Idle dawns in a row (any quest, exam or trial resets it).
        public int MiserableDays { get; set; } // NOTE: Miserable dawns in a row (departure after "Leave After Days").
        public SatisfactionLevel Level { get; set; } = SatisfactionLevel.Content; // NOTE: Last level set (for OnMoodChanged).

        public void Remember(MoodMemory memory)
        {
            _memories.RemoveAll(existing => existing.Reason == memory.Reason);
            if (memory.Amount != 0f && memory.EndsAt > memory.StartedAt)
                _memories.Add(memory);
        }

        // Drops memories that have fully faded.
        public void Forget(long nowMinutes) => _memories.RemoveAll(memory => memory.EndsAt <= nowMinutes);

        public SavedMood ToSaved() => new(AdventurerId, _memories.ToArray(), IdleDays, MiserableDays);

        // NOTE: Reasons that no longer exist (renamed later) are skipped; counters can't go negative.
        public static AdventurerMood FromSaved(SavedMood saved)
        {
            var mood = new AdventurerMood(saved.AdventurerId)
            {
                IdleDays = Math.Max(0, saved.IdleDays),
                MiserableDays = Math.Max(0, saved.MiserableDays)
            };
            foreach (var memory in saved.Memories)
            {
                if (Enum.IsDefined(typeof(MoodReason), memory.Reason))
                    mood.Remember(memory);
            }
            return mood;
        }
    }
}