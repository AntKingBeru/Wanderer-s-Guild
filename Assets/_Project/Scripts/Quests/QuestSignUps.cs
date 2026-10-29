// The hourly sign-up pass: each idle adventurer applies to the most appealing open quest they can (appeal ≥ 1, no cooldown).
using System;
using System.Collections.Generic;

namespace WanderersGuild
{
    public static class QuestSignUps
    {
        /// <param name="roster"></param>
        /// <param name="quests"></param>
        /// <param name="settings"></param>
        /// <param name="nowMinutes"></param>
        /// <param name="onJoined">Called for every application (quest, adventurer).</param>
        /// <returns>Quests whose party filled during this pass (now waiting for approval).</returns>
        public static List<Quest> Run(IReadOnlyList<Adventurer> roster, IReadOnlyList<Quest> quests, QuestSettings settings,
            long nowMinutes, Action<Quest, Adventurer> onJoined)
        {
            List<Quest> filled = new();

            foreach (var adventurer in roster)
            {
                if (adventurer.Status != AdventurerStatus.Idle)
                    continue;

                Quest best = null;
                var bestAppeal = 1f; // NOTE: Must at least meet their expectation.
                foreach (var quest in quests)
                {
                    if (!quest.CanApply(adventurer.Id, nowMinutes) || !QuestCalculator.Qualifies(adventurer, quest))
                        continue;

                    var appeal = QuestCalculator.Appeal(adventurer, quest, settings);
                    if (appeal < bestAppeal)
                        continue;

                    best = quest;
                    bestAppeal = appeal;
                }

                if (best == null || !best.TryAddMember(adventurer.Id))
                    continue;

                adventurer.SetStatus(AdventurerStatus.SignedUp);
                onJoined(best, adventurer);
                if (best.IsFull)
                    filled.Add(best);
            }

            return filled;
        }
    }
}