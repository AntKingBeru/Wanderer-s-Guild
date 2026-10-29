// Display strings for requests: time left ("4d", "5h", "<1h"), last-day check, rank badge letter, reward, category/source line.
using System.Globalization;

namespace WanderersGuild
{
    public static class RequestFormatter
    {
        // Whole days while a day or more is left, then whole hours, then "&lt;1h".
        public static string TimeLeft(long minutesLeft)
        {
            return minutesLeft switch
            {
                >= GameCalendar.MinutesPerDay => $"{minutesLeft / GameCalendar.MinutesPerDay}d",
                >= GameCalendar.MinutesPerHour => $"{minutesLeft / GameCalendar.MinutesPerHour}h",
                _ => "<1h"
            };
        }

        // True on the request's final in-game day (shown in red).
        public static bool IsLastDay(long minutesLeft) => minutesLeft < GameCalendar.MinutesPerDay;

        public static string BadgeLabel(GuildRank rank) => RankFormatter.BadgeLabel(rank);

        public static string Reward(RewardData reward) => $"{reward.Gold.ToString("N0", CultureInfo.InvariantCulture)}g";

        public static string CategoryAndSource(RequestData data) => $"{data.Category} · {data.Source}";

        public static string RankAndParty(RequestData data) => $"Rank {data.RecommendedRank} · Party of {data.RecommendedPartySize}";
    }
}