// All plain value-type data containers, grouped by system into regions.

using System;
using UnityEngine;

#region Adventurer System
[Serializable]
public struct StatBlock
{
    public int strength, dexterity, endurance, wits, spirit;

    public StatBlock(int strength, int dexterity, int endurance, int wits, int spirit)
    {
        this.strength = strength;
        this.dexterity = dexterity;
        this.endurance = endurance;
        this.wits = wits;
        this.spirit = spirit;
    }
    
    public int Total => strength + dexterity + endurance + wits + spirit;
    
    public static StatBlock operator +(StatBlock a, StatBlock b) =>
        new (a.strength + b.strength, a.dexterity + b.dexterity,
            a.endurance + b.endurance, a.wits + b.wits, a.spirit + b.spirit);

    public int Get(StatType type) => type switch
    {
        StatType.Strength => strength,
        StatType.Dexterity => dexterity,
        StatType.Endurance => endurance,
        StatType.Wits => wits,
        StatType.Spirit => spirit,
        _ => 0
    };
}
#endregion

#region Quest System
// Reward split between guild and adventurers.
[Serializable]
public struct RewardSplit
{
    [Range(0, 100)] public int guildPercent;
    
    public RewardSplit(int guildPercent) => this.guildPercent = Mathf.Clamp(guildPercent, 0, 100);
    
    public int AdventurerPercent => 100 - guildPercent;
}
// Concrete payout produced by a completed quest.
[Serializable]
public struct QuestReward
{
    public int gold;
    public int reputation;

    public QuestReward(int gold, int reputation)
    {
        this.gold = gold;
        this.reputation = reputation;
    }
}

// Minimum bar a party must clear to attempt a quest.
[Serializable]
public struct QuestRequirements
{
    public GuildRank minRank, maxRank;
    public int minPartySize, maxPartySize;
    public QuestCategory category;

    public QuestRequirements(GuildRank minRank, GuildRank maxRank,
        int minPartySize, int maxPartySize, QuestCategory category)
    {
        this.minRank = minRank;
        this.maxRank = maxRank;
        this.minPartySize = minPartySize;
        this.maxPartySize = maxPartySize;
        this.category = category;
    }
}

// Player-authored configuration produced by QuestBuilder and frozen into a Quest.
[Serializable]
public struct QuestConfiguration
{
    public RewardSplit rewardSplit;
    public GuildRank minRank, maxRank;
    public int minPartySize, maxPartySize;

    public QuestConfiguration(RewardSplit rewardSplit, GuildRank minRank, GuildRank maxRank,
        int minPartySize, int maxPartySize)
    {
        this.rewardSplit = rewardSplit;
        this.minRank = minRank;
        this.maxRank = maxRank;
        this.minPartySize = minPartySize;
        this.maxPartySize = maxPartySize;
    }
}

// Outcome of a resolved quest: success flag, scaled payouts, and any casualties.
[Serializable]
public struct QuestOutcome
{
    public bool success;
    public int goldToGuild;
    public int goldPerSurvivor;
    public int experiencePerMember;
    public int rankProgressPerMember;
    public int reputationDelta;

    public QuestOutcome(bool success, int goldToGuild, int goldPerSurvivor,
        int experiencePerMember, int rankProgressPerMember, int reputationDelta)
    {
        this.success = success;
        this.goldToGuild = goldToGuild;
        this.goldPerSurvivor = goldPerSurvivor;
        this.experiencePerMember = experiencePerMember;
        this.rankProgressPerMember = rankProgressPerMember;
        this.reputationDelta = reputationDelta;
    }
}
#endregion

#region Party System
// Allowed party-size window (varies by rank band).
[Serializable]
public struct PartySizeRange
{
    public int min, max;

    public PartySizeRange(int min, int max)
    {
        this.min = min;
        this.max = max;
    }
    
    public bool Contains(int count) => count >= min && count <= max;
}
#endregion

#region Economy System
// A single recorded gold movement.
[Serializable]
public struct Transaction
{
    public TransactionType type;
    public int amount;
    
    public Transaction(TransactionType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}
#endregion

#region Time & Simulation System
// Calendar stamp used across the simulation; sortable chronologically.
[Serializable]
public struct GameDate : IComparable<GameDate>
{
    public int year;
    public Season season;
    public int day;

    public GameDate(int year, Season season, int day)
    {
        this.year = year;
        this.season = season;
        this.day = day;
    }

    public int CompareTo(GameDate other)
    {
        if (year != other.year)
            return year.CompareTo(other.year);
        return season != other.season
            ? ((int)season).CompareTo((int)other.season)
            : day.CompareTo(other.day);
    }

    public GameDate AddDays(int days, int daysPerSeason)
    {
        daysPerSeason = Mathf.Max(1, daysPerSeason);
        var y = year;
        var s = season;
        var d = day + Mathf.Max(0, days);

        while (d > daysPerSeason)
        {
            d -= daysPerSeason;
            if (s == Season.Winter)
            {
                s = Season.Spring;
                y++;
            }
            else
                s = (Season)((int)s + 1);
        }
        return new GameDate(y, s, d);
    }
    
    public override string ToString() => $"Y{year} {season} D{day}";
}
#endregion

#region Lightning System
[Serializable]
public struct LightingSample
{
    public Quaternion sunRotation;
    public Color sunColor;
    public Color ambientColor;
    public float sunIntensity;

    public LightingSample(Quaternion sunRotation, Color sunColor, Color ambientColor, float sunIntensity)
    {
        this.sunRotation = sunRotation;
        this.sunColor = sunColor;
        this.ambientColor = ambientColor;
        this.sunIntensity = sunIntensity;
    }

    public static LightingSample Lerp(LightingSample a, LightingSample b, float t) =>
        new LightingSample
        (
            Quaternion.Lerp(a.sunRotation, b.sunRotation, t),
            Color.Lerp(a.sunColor, b.sunColor, t),
            Color.Lerp(a.ambientColor, b.ambientColor, t),
            Mathf.Lerp(a.sunIntensity, b.sunIntensity, t)
        );
}
#endregion