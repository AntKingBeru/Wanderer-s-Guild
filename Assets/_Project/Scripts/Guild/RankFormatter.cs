// Rank display shared by every UI: the one-letter badge label (National shows "N").
namespace WanderersGuild
{
    public static class RankFormatter
    {
        // One letter per badge: F … S, and "N" for National.
        public static string BadgeLabel(GuildRank rank) => rank == GuildRank.National ? "N" : rank.ToString();
    }
}