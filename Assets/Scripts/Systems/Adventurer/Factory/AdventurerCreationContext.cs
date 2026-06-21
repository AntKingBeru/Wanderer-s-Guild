// Plain data object built by AdventurerManager before each creation call and handed to the
// active factory. Keeps factories decoupled from manager internals — they read roster
// composition through this snapshot instead of reaching into AdventurerManager directly.

using System;

public class AdventurerCreationContext
{
    // Current guild rank ceiling — informs rank/class weighting.
    public QuestRank GuildRankCap { get; set; }

    // Total adventurers currently registered in the guild.
    public int TotalAdventurerCount { get; set; }

    // Count of adventurers per class (indexed by AdventurerClass enum int value).
    // Reserved for future factories that want to balance class distribution.
    public int[] AdventurersPerClass { get; set; }

    // Count of adventurers per rank (indexed by QuestRank enum int value).
    // Reserved for future factories that want to balance rank distribution.
    public int[] AdventurersPerRank { get; set; }

    public AdventurerCreationContext()
    {
        AdventurersPerClass = new int[Enum.GetValues(typeof(AdventurerClass)).Length];
        AdventurersPerRank = new int[Enum.GetValues(typeof(QuestRank)).Length];
    }
}