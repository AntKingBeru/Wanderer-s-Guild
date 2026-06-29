// Singleton holding the guild's current rank and the rank-derived quest-board slot capacity.

using UnityEngine;

[DefaultExecutionOrder(-85)]
public class GuildController : MonoSingleton<GuildController>
{
    public GuildRank CurrentRank { get; private set; }
    
    public int BoardSlotCount => (int)CurrentRank + GameConfig.Instance.Guild.boardSlotBase;
    
    protected override void OnSingletonAwake()
        => CurrentRank = GameConfig.Instance.Guild.startingRank;
    
    public bool SetRank(GuildRank rank)
    {
        if (rank == CurrentRank)
            return false;
        CurrentRank = rank;
        GameEventsRelay.Instance.RaiseGuildRankChanged(rank);
        return true;
    }
}