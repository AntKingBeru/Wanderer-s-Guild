// Central enumeration definitions for all systems. Each system adds its enums under its region.
namespace WanderersGuild
{
    #region Shared
    // Unified rank scale — Guild Rank and Adventurer Rank progress identically (F low → National high).
    public enum Rank
    {
        F,
        E,
        D,
        C,
        B,
        A,
        S,
        National
    }
    #endregion

    #region Time
    public enum GameSpeed
    {
        Paused,
        Normal,
        Fast,
        VeryFast
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
    #endregion
    
    #region Quest
    // PLACEHOLDER category set — drives class affinity + resolution. Confirm real list (see note below).
    public enum QuestCategory
    {
        Combat,
        Gathering,
        Escort,
        Delivery,
        Subjugation,
        Exploration,
        Investigation,
        Dungeon
    }

    public enum Difficulty
    {
        Trivial,
        Easy,
        Moderate,
        Hard,
        Severe,
        Deadly
    }

    // Request origin (grounded in HLD's request-source list).
    public enum RequestSource
    {
        Kingdom,
        Nobility,
        Merchant,
        Traveler,
        Settlement,
        Organization
    }
    #endregion

    // Adventurer, Facility, Economy regions added as those systems come online.
}