// Plain data object passed into the factory before each adventurer is created.
// AdventurerManager builds this from the current game state every time the arrival timer fires.
// Future systems (reputation, events, seasons) add fields here.

using System;

public class AdventurerCreationContext
{
    // The current guild rank ceiling - factory clamps adventurer rank to cap +1.
    public QuestRank GuildRankCap { get; set; }
    
    // Total adventurers currently registered in the guild.
    public int TotalAdventurerCount { get; set; }
    
    // Count of adventurers per class (indexed by AdventurerClass enum int value).
    // Reserved for future factories that want to balance class distribution.
    public int[] AdventurersPerClass { get; set; }
    
    // Count of adventurers per rank (indexed by QuestRank enum int value).
    // Reserved for future factories that want to balance rank distribution.
    public int[] AdventurersPerRank { get; set; }
    
    // Future hooks - set to neutral values until the relative systems are built:
    // public float ReputationModifier { get; set; } = 1f;
    // public Season CurrentSeason { get; set; } = Season.Spring;

    public AdventurerCreationContext()
    {
        var classCount = Enum.GetValues(typeof(AdventurerClass)).Length;
        var rankCount = Enum.GetValues(typeof(QuestRank)).Length;
        AdventurersPerClass = new int[classCount];
        AdventurersPerRank = new int[rankCount];
    }
}