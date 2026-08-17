// Central struct definitions for all systems. Each system adds its structs under its region.
using System;

namespace WanderersGuild
{
    #region Time
    // Lightweight calendar moment, produced by the time system.
    [Serializable]
    public struct GameDate
    {
        public int day;
        public int month;
        public int year;
        public Season season;

        public GameDate(int day, int month, int year, Season season)
        {
            this.day = day;
            this.month = month;
            this.year = year;
            this.season = season;
        }
    }
    #endregion
    
    #region Stats
    // PLACEHOLDER stat vocabulary. Used by species mods, class base/growth, and (later) quest requirements.
    [Serializable]
    public struct StatBlock
    {
        public int strength;
        public int agility;
        public int intellect;
        public int vitality;

        public StatBlock(int strength, int agility, int intellect, int vitality)
        {
            this.strength = strength;
            this.agility = agility;
            this.intellect = intellect;
            this.vitality = vitality;
        }

        // NOTE: Combine blocks (e.g. classBase + speciesMods).
        public static StatBlock operator +(StatBlock a, StatBlock b)
            => new(a.strength + b.strength, a.agility + b.agility, a.intellect + b.intellect, a.vitality + b.vitality);

        // NOTE: Scale a block (e.g. growthPerLevel * level).
        public static StatBlock operator *(StatBlock a, int scalar)
            => new(a.strength * scalar, a.agility * scalar, a.intellect * scalar, a.vitality * scalar);
    }
    #endregion
    
    #region Quest
    // Reward payload on a request/quest. Item/artifact rewards get added here later.
    [Serializable]
    public struct RewardData
    {
        public int gold;
    }
    #endregion

    // Adventurer, Economy regions added as those systems come online.
}