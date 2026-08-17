// Central relay of global UnityEvents plus their concrete event types. Grouped by system region.
using System;
using UnityEngine.Events;

namespace WanderersGuild
{
    // Generic UnityEvent<T> must be subclassed to be instantiable/serializable.
    #region Event Types
    [Serializable] public class GameDateEvent : UnityEvent<GameDate> { }
    [Serializable] public class SeasonEvent : UnityEvent<Season> { }
    [Serializable] public class GameSpeedEvent : UnityEvent<GameSpeed> { }
    [Serializable] public class GuildRankEvent : UnityEvent<Rank> { }
    [Serializable] public class IntEvent : UnityEvent<int> { }
    #endregion

    // Persistent singleton (tick Don't Destroy On Load). Access via GameEventsRelay.Instance.
    public class GameEventsRelay : Singleton<GameEventsRelay>
    {
        #region Time
        public IntEvent onTick = new();
        public IntEvent onHourChanged = new();
        public GameDateEvent onDayChanged = new();
        public SeasonEvent onSeasonChanged = new();
        public GameSpeedEvent onGameSpeedChanged = new();
        #endregion

        #region Guild
        public GuildRankEvent onGuildRankChanged = new();
        public IntEvent onReputationChanged = new();
        #endregion

        // Quest, Adventurer, Facility, Economy regions added as those systems come online.
    }
}