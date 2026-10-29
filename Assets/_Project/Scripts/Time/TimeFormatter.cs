// Display strings for in-game time: full date, short date (save slots), clock, and day phase.
namespace WanderersGuild
{
    public static class TimeFormatter
    {
        // "Spring 3, Year 1"
        public static string Date(GameDateTime time) => $"{MonthName(time.Month)} {time.Day}, Year {time.Year}";

        // "Spring 3, Y1" — compact, for the save-slot list.
        public static string ShortDate(GameDateStamp date) => $"{MonthName(date.Month)} {date.Day}, Y{date.Year}";

        // "08:24"
        public static string Clock(GameDateTime time) => $"{time.Hour:00}:{time.Minute:00}";

        public static string Phase(DayPhase phase) => phase.ToString();

        // NOTE: Falls back to "M5" etc. when the calendar has no such month (e.g. old test saves).
        private static string MonthName(int month) =>
            GameConfig.HasInstance ? GameConfig.Instance.Time.MonthName(month) : $"M{month}";
    }
}