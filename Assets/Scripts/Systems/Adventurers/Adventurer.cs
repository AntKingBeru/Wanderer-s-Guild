// Pure runtime model of an adventurer: identity, stats, progression, state, and personal gold.

public class Adventurer
{
    public int Id { get; }
    public string Name { get; }
    public AdventurerClass Class { get; private set; }
    public ClassTier Tier { get; private set; }
    public GuildRank Rank { get; private set; }

    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int RankProgress { get; private set; }

    public StatBlock Stats { get; private set; }
    public AdventurerState State { get; private set; } = AdventurerState.Idle;
    public int Gold { get; private set; }

    private readonly StatBlock _growthPerLevel;
    private readonly int _experiencePerLevelBase;
    
    public Adventurer(int id, string name, AdventurerClass adventurerClass, ClassTier tier,
        GuildRank rank, StatBlock baseStats, StatBlock growthPerLevel,
        int startingGold, int experiencePerLevelBase)
    {
        Id = id;
        Name = name;
        Class = adventurerClass;
        Tier = tier;
        Rank = rank;
        Stats = baseStats;
        _growthPerLevel = growthPerLevel;
        Gold = startingGold;
        _experiencePerLevelBase = System.Math.Max(1, experiencePerLevelBase);
    }
    
    public int ExperienceForNextLevel => _experiencePerLevelBase * Level;
    
    public int AddExperience(int amount)
    {
        if (amount <= 0)
            return 0;
        Experience += amount;
        var gained = 0;
        while (Experience >= ExperienceForNextLevel)
        {
            Experience -= ExperienceForNextLevel;
            Level++;
            Stats += _growthPerLevel;
            gained++;
        }
        return gained;
    }

    public void AddRankProgress(int amount)
    {
        if (amount > 0) RankProgress += amount;
    }
    
    public bool TryPromote(GuildRank cap)
    {
        if (Rank >= cap || Rank >= GuildRank.National)
            return false;
        Rank = (GuildRank)((int)Rank + 1);
        RankProgress = 0;
        return true;
    }

    public void SetState(AdventurerState next)
        => State = next;

    public void AddGold(int amount)
    {
        Gold += amount;
        if (Gold < 0)
            Gold = 0;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount)
            return false;
        Gold -= amount;
        return true;
    }
}