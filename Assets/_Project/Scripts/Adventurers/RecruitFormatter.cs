// Display strings for the Recruits screen and desk: title, row info, time left, "leaves in", full-guild hint, desk line.
namespace WanderersGuild
{
    public static class RecruitFormatter
    {
        public static string Title(int waiting, int slots) => $"Recruits {waiting}/{slots}";

        // "Fighter Lv 2".
        public static string RowInfo(Adventurer adventurer) => $"{adventurer.Class.DisplayName} Lv {adventurer.Level}";

        public static long MinutesLeft(Applicant applicant, long nowMinutes) => System.Math.Max(0L, applicant.ExpiresAtMinutes - nowMinutes);

        // "2d" / "5h" (RequestFormatter's style).
        public static string TimeLeft(Applicant applicant, long nowMinutes) => RequestFormatter.TimeLeft(MinutesLeft(applicant, nowMinutes));

        public static string LeavesIn(Applicant applicant, long nowMinutes) => $"Leaves in {TimeLeft(applicant, nowMinutes)}";

        public static bool IsLastDay(Applicant applicant, long nowMinutes) => RequestFormatter.IsLastDay(MinutesLeft(applicant, nowMinutes));

        // Empty when there's a bed; otherwise "Guild is full (6/6)".
        public static string FullHint(int members, int capacity) => members >= capacity ? $"Guild is full ({members}/{capacity})" : string.Empty;

        // Reception desk: "Recruits: 2 waiting at the office" / "Recruits: nobody waiting".
        public static string DeskLine(int waiting) =>
            waiting == 0 ? "Recruits: nobody waiting" : $"Recruits: {waiting} waiting at the office";
    }
}