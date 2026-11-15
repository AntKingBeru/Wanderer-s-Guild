// The live situations behind a mood: lodging outside or in their own bed, and idling too long without a quest.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Recomputed every time (never stored) — moving into a bed helps at once. Adds each active situation to entries
    // (when given) and returns their total.
    public static class MoodSituations
    {
        public static float Collect(Adventurer adventurer, AdventurerMood mood, SatisfactionSettings settings, List<MoodEntry> entries)
        {
            var total = 0f;
            if (LodgingManager.HasInstance)
            {
                var outside = LodgingManager.Instance.LodgesOutside(adventurer.Id);
                total += Add(outside ? MoodReason.LodgingOutside : MoodReason.OwnBed,
                    outside ? settings.LodgingOutside : settings.OwnBed, entries);
            }

            var overIdle = mood.IdleDays - settings.IdleGraceDays;
            if (overIdle > 0)
                total += Add(MoodReason.IdleTooLong, Mathf.Max(-settings.IdleMaxPenalty, settings.IdlePerDay * overIdle), entries);
            return total;
        }

        private static float Add(MoodReason reason, float value, List<MoodEntry> entries)
        {
            if (value == 0f)
                return 0f;
            entries?.Add(new MoodEntry(reason, value));
            return value;
        }
    }
}