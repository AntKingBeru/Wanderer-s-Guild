// Pure helper: computes the selectable adventurer-rank range around a request's recommended rank.

using UnityEngine;

public static class RankRange
{
    private const int MinRankIndex = (int)GuildRank.F;
    private const int MaxRankIndex = (int)GuildRank.National;
    
    public static (GuildRank min, GuildRank max) For(GuildRank recommended)
    {
        var rec = (int)recommended;

        var min = Mathf.Max(MinRankIndex, rec - 1);

        var ceilingCandidate = rec >= (int)GuildRank.A ? rec : rec + 1;
        var max = Mathf.Min(MaxRankIndex, ceilingCandidate);

        return ((GuildRank)min, (GuildRank)max);
    }
    
    public static System.Collections.Generic.List<GuildRank> Options(GuildRank recommended)
    {
        var (min, max) = For(recommended);
        var list = new System.Collections.Generic.List<GuildRank>();
        for (var i = (int)min; i <= (int)max; i++)
            list.Add((GuildRank)i);
        return list;
    }
}