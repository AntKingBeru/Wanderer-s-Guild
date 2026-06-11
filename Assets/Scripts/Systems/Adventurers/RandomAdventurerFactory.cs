// Concrete factory that generates adventurers with randomized class, rank, and level.
// Class is chosen uniformly from AdventurerConfig's pool.
// Rank is weighted via AdventurerConfig.GetRandomStartingRank().
// Level is drawn from a range per rank (the placeholder values below; intended to move to AdventurerConfig once the balance sheet is ready).

using UnityEngine;

public class RandomAdventurerFactory : AdventurerFactory
{
    private readonly AdventurerConfig _config;
    
    // Simple counter for placeholder names. Static so it persists across factory instances within a session.
    // Replaced entirely when the name generator is added.
    private static int _nameCounter;
    
    // Starting level range per rank (indexed by QuestRank int value).
    // Future: move to AdventurerConfig for inspector-controlled balance tuning.
    private static readonly int[] StartLevelMin = { 1, 4, 9, 16, 26, 37, 46, 50 };
    private static readonly int[] StartLevelMax = { 3, 8, 15, 25, 36, 45, 49, 50 };
    
    public RandomAdventurerFactory(AdventurerConfig config)
    {
        if (!config)
            Debug.LogError("[RandomAdventurerFactory] AdventurerConfig is null. " +
                           "Assign it in AdventurerManager's inspector.");
        _config = config;
    }
    
    public override AdventurerData CreateAdventurer(AdventurerCreationContext context)
    {
        if (!_config)
            return null;
        // Class
        var classData = _config.GetRandomClassData();
        if (!classData)
        {
            Debug.LogError("[RandomAdventurerFactory] No ClassData in the pool. " +
                           "Assign ClassData assets to AdventurerConfig.");
            return null;
        }
        // Rank
        var rank = _config.GetRandomStartingRank();
        // Level
        var rankIndex = Mathf.Clamp((int)rank, 0, StartLevelMin.Length - 1);
        var level = Random.Range(StartLevelMin[rankIndex], StartLevelMax[rankIndex] + 1);
        level = Mathf.Clamp(level, 1, _config.MaxLevel);
        // Name (placeholder)
        var name = $"{classData.DisplayName} #{++_nameCounter:D4}";
        // TODO: Replace with a proper random name generator.
        return new AdventurerData(GenerateID(), name, classData, rank, level, _config);
    }
}