// Turns save-header values into the short labels the slot list shows (rank, in-game date, play time, save time).
using System;
using System.Globalization;

namespace WanderersGuild
{
    public static class SaveSlotFormatter
    {
        public static string Rank(GuildRank rank) => rank == GuildRank.National ? "National" : $"Rank {rank}";

        // "Spring 3, Y1" — uses the calendar in GameConfig › Time.
        public static string Date(GameDateStamp date) => TimeFormatter.ShortDate(date);

        public static string PlayTime(double seconds)
        {
            var total = (long)Math.Clamp(seconds, 0d, 3.6e9);
            return $"{total / 3600}h {total % 3600 / 60:00}m";
        }

        // NOTE: A bad format string in GameConfig falls back to the culture's short date/time instead of throwing.
        public static string SavedAt(DateTime local, string format)
        {
            try
            {
                return local.ToString(format, CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                return local.ToString("g", CultureInfo.CurrentCulture);
            }
        }
    }
}