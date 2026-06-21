// Concrete factory (Factory Method pattern) that generates adventurers with randomized
// rank, level, class, and name. Rank comes from AdventurerConfig's weighted distribution,
// level scales with rank, and class is rolled from ClassRegistry's currently-unlocked pool
// (filtered to classes whose MinimumLevel the rolled level actually satisfies).

using UnityEngine;

public class RandomAdventurerFactory : AdventurerFactory
{
    private readonly AdventurerConfig _config;
    private readonly RandomNameGenerator _nameGenerator;

    public RandomAdventurerFactory(AdventurerConfig config, RandomNameGenerator nameGenerator)
    {
        _config = config;
        _nameGenerator = nameGenerator;
        if (!_config)
            Debug.LogError("[RandomAdventurerFactory] AdventurerConfig is null.");
    }
    
    public override AdventurerData CreateAdventurer(AdventurerCreationContext context)
    {
        if (!_config)
            return null;
        if (!ClassRegistry.Instance)
        {
            Debug.LogError("[RandomAdventurerFactory] ClassRegistry not found in scene.");
            return null;
        }

        var rank = _config.GetRandomStartingRank();
        var level = _config.GetRandomLevelForRank(rank);

        var classData = ClassRegistry.Instance.GetRandomUnlockedClassData(rank, level);
        if (!classData)
            return null;

        var name = _nameGenerator.GenerateName();

        return new AdventurerBuilder(_config)
            .WithId(GenerateID())
            .WithName(name)
            .WithClass(classData)
            .WithRank(rank)
            .WithLevel(level)
            .Build();
    }
}