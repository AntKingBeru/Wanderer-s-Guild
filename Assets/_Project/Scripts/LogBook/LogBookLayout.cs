// Turns the log's entries into the list's rows: newest or oldest first, with a date header whenever the day changes.
using System.Collections.Generic;

namespace WanderersGuild
{
    public static class LogBookLayout
    {
        /// <param name="entries">Oldest first (GuildLog.Entries).</param>
        /// <param name="newestFirst"></param>
        /// <param name="lines">Cleared and refilled.</param>
        public static void Build(IReadOnlyList<LogEntry> entries, bool newestFirst, List<LogLine> lines)
        {
            lines.Clear();
            var calendar = TimeManager.HasInstance ? TimeManager.Instance.Calendar : null;
            var lastDay = long.MinValue;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[newestFirst ? entries.Count - 1 - i : i];
                var day = entry.Minutes / GameCalendar.MinutesPerDay;
                if (day != lastDay)
                {
                    lastDay = day;
                    lines.Add(new LogLine(true, string.Empty, DayLabel(entry.Minutes, calendar), LogTone.Normal));
                }

                lines.Add(new LogLine(false, Clock(entry.Minutes), entry.Text, entry.Tone));
            }
        }

        // "Spring 4, Year 1" — or "Day 12" without a calendar.
        private static string DayLabel(long minutes, GameCalendar calendar) =>
            calendar != null
                ? TimeFormatter.Date(calendar.FromTotalMinutes(minutes))
                : $"Day {minutes / GameCalendar.MinutesPerDay + 1}";

        // "14:05"
        private static string Clock(long minutes)
        {
            var minuteOfDay = (int)(minutes % GameCalendar.MinutesPerDay);
            return $"{minuteOfDay / GameCalendar.MinutesPerHour:00}:{minuteOfDay % GameCalendar.MinutesPerHour:00}";
        }
    }
}