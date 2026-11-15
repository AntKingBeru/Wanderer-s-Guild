// The hourly sign-up pass: parties apply together (room for all, appealing to every member), then loners apply to the most
// appealing open quest they can — appeal measured against what their mood asks for (1 = normal; Unhappy ask more).
using System;
using System.Collections.Generic;
using System.Linq;

namespace WanderersGuild
{
    public static class QuestSignUps
    {
        /// <param name="roster"></param>
        /// <param name="quests"></param>
        /// <param name="parties"></param>
        /// <param name="find">Adventurer lookup by id (party members).</param>
        /// <param name="settings"></param>
        /// <param name="nowMinutes"></param>
        /// <param name="onJoined">Called for every application (quest, adventurer).</param>
        /// <param name="requiredAppeal">The appeal each adventurer needs (null = 1 for everyone).</param>
        /// <returns>Quests whose party filled during this pass (now waiting for approval) — each once.</returns>
        public static List<Quest> Run(IReadOnlyList<Adventurer> roster, IReadOnlyList<Quest> quests, IReadOnlyList<GuildParty> parties,
                                      Func<int, Adventurer> find, QuestSettings settings, long nowMinutes,
                                      Action<Quest, Adventurer> onJoined, Func<Adventurer, float> requiredAppeal = null)
        {
            List<Quest> filled = new();
            HashSet<Quest> filledSet = new();

            foreach (var party in parties)
                SignUpParty(party, find, quests, settings, nowMinutes, onJoined, requiredAppeal, filled, filledSet);

            foreach (var adventurer in roster)
            {
                if (adventurer.Status != AdventurerStatus.Idle || adventurer.IsInParty)
                    continue;

                Quest best = null;
                var bestAppeal = 1f; // NOTE: Appeal ÷ what they require — must at least meet it.
                foreach (var quest in quests)
                {
                    if (!quest.CanApply(adventurer.Id, nowMinutes) || !QuestCalculator.Qualifies(adventurer, quest))
                        continue;

                    var appeal = RelativeAppeal(adventurer, quest, settings, requiredAppeal);
                    if (appeal < bestAppeal)
                        continue;

                    best = quest;
                    bestAppeal = appeal;
                }

                if (best == null || !best.TryAddMember(adventurer.Id))
                    continue;
                Joined(best, adventurer, onJoined, filled, filledSet);
            }

            return filled;
        }

        // NOTE: Everyone must be idle (one injured member holds the party back); the quest needs a free slot for each of them,
        // and each must be allowed, qualify and find it appealing enough for their mood. The best quest maximizes the
        // least-satisfied member's relative appeal.
        private static void SignUpParty(GuildParty party, Func<int, Adventurer> find, IReadOnlyList<Quest> quests,
                                        QuestSettings settings, long nowMinutes, Action<Quest, Adventurer> onJoined,
                                        Func<Adventurer, float> requiredAppeal, List<Quest> filled, HashSet<Quest> filledSet)
        {
            List<Adventurer> members = new();
            foreach (var id in party.MemberIds)
            {
                var member = find(id);
                if (member is not { Status: AdventurerStatus.Idle })
                    return;
                members.Add(member);
            }

            Quest best = null;
            var bestAppeal = 1f;
            foreach (var quest in quests)
            {
                if (quest.State != QuestState.Waiting || quest.Terms.PartySize - quest.MemberIds.Count < members.Count)
                    continue;

                var worst = float.MaxValue;
                foreach (var member in members)
                {
                    if (!quest.CanApply(member.Id, nowMinutes) || !QuestCalculator.Qualifies(member, quest))
                    {
                        worst = 0f;
                        break;
                    }
                    worst = Math.Min(worst, RelativeAppeal(member, quest, settings, requiredAppeal));
                }

                if (worst < bestAppeal)
                    continue;
                best = quest;
                bestAppeal = worst;
            }

            if (best == null)
                return;
            foreach (var member in members.Where(member => best.TryAddMember(member.Id)))
            {
                Joined(best, member, onJoined, filled, filledSet);
            }
        }

        // NOTE: ≥ 1 = willing. A required appeal below 1 is treated as 1 (nobody signs up below their expectation).
        private static float RelativeAppeal(Adventurer adventurer, Quest quest, QuestSettings settings,
                                            Func<Adventurer, float> requiredAppeal)
        {
            var required = requiredAppeal != null ? Math.Max(1f, requiredAppeal(adventurer)) : 1f;
            return QuestCalculator.Appeal(adventurer, quest, settings) / required;
        }

        private static void Joined(Quest quest, Adventurer adventurer, Action<Quest, Adventurer> onJoined,
                                   List<Quest> filled, HashSet<Quest> filledSet)
        {
            adventurer.SetStatus(AdventurerStatus.SignedUp);
            onJoined(quest, adventurer);
            if (quest.IsFull && filledSet.Add(quest))
                filled.Add(quest);
        }
    }
}