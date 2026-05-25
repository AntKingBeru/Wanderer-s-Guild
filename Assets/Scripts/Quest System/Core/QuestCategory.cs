using System.Collections.Generic;

namespace QuestSystem
{
    /// <summary>
    /// All available quest categories.
    /// Each category carries associated tags used in later-stage quest filtering/matching.
    /// </summary>
    public enum QuestCategory
    {
        Combat,
        Gathering,
        Exploration,
        Escort,
        Delivery,
        Investigation,
        Crafting,
        Dungeon
    }
    
    /// <summary>
    /// Maps every QuestCategory to its associated tags.
    /// Tags are shown during quest creation (request → quest) but NOT stored on the final quest.
    /// </summary>
    public static class QuestCategoryTags
    {
        private static readonly Dictionary<QuestCategory, List<string>> Tags =
            new()
            {
                { QuestCategory.Combat, new List<string> { "Fighting", "Monsters", "PvE", "Danger" } },
                { QuestCategory.Gathering, new List<string> { "Resources", "Farming", "Collecting", "Outdoors" } },
                { QuestCategory.Exploration, new List<string> { "Travel", "Discovery", "Mapping", "Outdoors" } },
                { QuestCategory.Escort, new List<string> { "Protection", "NPC", "Travel", "Danger" } },
                { QuestCategory.Delivery, new List<string> { "Transport", "Trade", "NPC", "Time-Sensitive" } },
                { QuestCategory.Investigation, new List<string> { "Mystery", "Clues", "Stealth", "Social" } },
                { QuestCategory.Crafting, new List<string> { "Resources", "Skills", "Workshop", "Trade" } },
                { QuestCategory.Dungeon, new List<string> { "Fighting", "Discovery", "Teamwork", "High-Risk" } }
            };

        /// <summary>
        /// Returns the tag list for the given category.
        /// </summary>
        public static List<string> GetTags(QuestCategory category)
        {
            return Tags.TryGetValue(category, out var tags) ? tags : new List<string>();
        }
    }
}