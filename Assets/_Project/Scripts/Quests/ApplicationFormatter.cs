// Display strings for the reception desk and applications: desk detail lines, row status, applicant line, approve hint.
namespace WanderersGuild
{
    public static class ApplicationFormatter
    {
        // Requests half: "4 new · 6 on the board", "6 on the board", or "No requests".
        public static string DeskRequestsDetail(int total, int unseen)
        {
            if (total == 0)
                return "No requests";
            return unseen > 0 ? $"{unseen} new · {total} on the board" : $"{total} on the board";
        }

        // Applications half: "2 ready · 5 applicants", "5 applicants", or "No applicants".
        public static string DeskApplicationsDetail(int ready, int applicants)
        {
            if (applicants == 0)
                return "No applicants";
            var people = applicants == 1 ? "1 applicant" : $"{applicants} applicants";
            return ready > 0 ? $"{ready} ready · {people}" : people;
        }

        // List status: "READY" when full, otherwise "2/3".
        public static string RowStatus(Quest quest) =>
            quest.State == QuestState.Ready ? "READY" : $"{quest.MemberIds.Count}/{quest.Terms.PartySize}";

        // "Bran · Fighter · Lv 3 · F".
        public static string Applicant(Adventurer adventurer) =>
            $"{adventurer.Name} · {adventurer.Class.DisplayName} · Lv {adventurer.Level} · {RankFormatter.BadgeLabel(adventurer.Rank)}";

        // "Expires in 2d · Success: 64%".
        public static string TimeAndChance(Quest quest, float chance, bool hasMembers, long nowMinutes) =>
            $"{QuestFormatter.TimeLine(quest, nowMinutes)} · {QuestFormatter.Chance(chance, hasMembers)}";

        // Empty when ready, otherwise "Waiting for 1 more" / "Waiting for 2 more".
        public static string ApproveHint(Quest quest)
        {
            var missing = quest.Terms.PartySize - quest.MemberIds.Count;
            return quest.State == QuestState.Ready || missing <= 0 ? string.Empty : $"Waiting for {missing} more";
        }
    }
}