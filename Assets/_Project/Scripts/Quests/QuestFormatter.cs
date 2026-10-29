// Display strings for quests: gold split, guild share, duration, note reward/time/placeholder, timeline, terms, success chance.
using System.Globalization;
using UnityEngine;

namespace WanderersGuild
{
    public static class QuestFormatter
    {
        // "Party 140g (47g each) · Guild 60g" — the guild also keeps the rounding remainder.
        public static string GoldSplit(RequestData request, QuestTerms terms)
        {
            var total = request.Reward.Gold;
            var party = (int)((long)total * terms.RewardSharePercent / 100);
            var each = party / System.Math.Max(1, terms.PartySize);
            var guild = total - each * terms.PartySize;
            return $"Party {Number(party)}g ({Number(each)}g each) · Guild {Number(guild)}g";
        }

        // The guild's gold from a success with this many members (same rule as QuestManager's payout).
        public static long GuildShare(Quest quest, int memberCount)
        {
            var members = System.Math.Max(1, memberCount);
            return quest.Request.Reward.Gold - (long)(quest.PartyGold / members) * members;
        }

        public static string Duration(int days) => days == 1 ? "About 1 day" : $"About {days} days";

        // Placeholder for a missing category icon: the first two letters ("Co", "Ga", "Ex"…), unique per category.
        public static string CategoryShort(QuestCategory category)
        {
            var name = category.ToString();
            return name.Length <= 2 ? name : name[..2];
        }

        // The reward written on a pinned note: the party's share ("140g").
        public static string NoteReward(Quest quest) => $"{Number(quest.PartyGold)}g";

        // The time on a pinned note: "2d" left before departure (waiting or ready), "Back 1d" while away.
        public static string NoteTime(Quest quest, long nowMinutes)
        {
            var left = RequestFormatter.TimeLeft(MinutesLeft(quest, nowMinutes));
            return quest.IsBeforeDeparture ? left : $"Back {left}";
        }

        // "Expires in 2d" before departure, "Back in 1d" while away.
        public static string TimeLine(Quest quest, long nowMinutes) =>
            quest.IsBeforeDeparture
                ? $"Expires in {RequestFormatter.TimeLeft(MinutesLeft(quest, nowMinutes))}"
                : $"Back in {RequestFormatter.TimeLeft(MinutesLeft(quest, nowMinutes))}";

        // True on the last day before an undeparted quest expires (shown in red).
        public static bool IsUrgent(Quest quest, long nowMinutes) =>
            quest.IsBeforeDeparture && RequestFormatter.IsLastDay(MinutesLeft(quest, nowMinutes));

        public static string Meta(RequestData request) =>
            $"{request.Category} · {request.Source} · Rank {request.RecommendedRank}";

        public static string Terms(Quest quest) => $"Min. rank {quest.Terms.MinRank} · Party of {quest.Terms.PartySize}";

        public static string Chance(float chance, bool hasMembers) =>
            hasMembers ? $"Success: {Mathf.RoundToInt(chance * 100f)}%" : "Success: —";

        // The stamp on a note: READY (full, awaiting approval), TAKEN (away), or none.
        public static string Stamp(Quest quest) =>
            quest.State switch
            {
                QuestState.Ready => "READY",
                QuestState.InProgress => "TAKEN",
                _ => string.Empty
            };

        // NOTE: Clamped at 0 for the moment between a quest coming due and the tick that handles it.
        public static long MinutesLeft(Quest quest, long nowMinutes) =>
            System.Math.Max(0L, (quest.IsBeforeDeparture ? quest.ExpiresAtMinutes : quest.ReturnsAtMinutes) - nowMinutes);

        private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}