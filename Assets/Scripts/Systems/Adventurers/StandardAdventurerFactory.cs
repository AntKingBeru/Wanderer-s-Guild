// Factory pattern: assembles Adventurer instances from class templates with randomized stats.

public class StandardAdventurerFactory : IAdventurerFactory
{
    private readonly System.Random _rng;
    private readonly NamePool _namePool;
    private readonly int _experiencePerLevelBase;

    public StandardAdventurerFactory(System.Random rng, NamePool namePool, int experiencePerLevelBase)
    {
        _rng = rng;
        _namePool = namePool;
        _experiencePerLevelBase = experiencePerLevelBase;
    }

    public Adventurer Create(int id, AdventurerClassTemplate template, GuildRank rank)
    {
        var name = _namePool ? _namePool.GenerateName(_rng) : $"Adventurer {id}";
        var baseStats = template.RollBaseStats(_rng);
        var gold = template.RollStartingGold(_rng);

        return new Adventurer(id, name, template.Class, template.Tier, rank,
            baseStats, template.GrowthPerLevel, gold, _experiencePerLevelBase);
    }
}