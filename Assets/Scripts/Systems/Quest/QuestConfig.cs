// ScriptableObject that centralizes every designer-tunable constant in the quest system.
// One instance lives in Assets/Data/Quest/ and is referenced by QuestManager.
// RankConfig is a struct (not a class), so it shares this file without violating the one-class-per-script rule.

using System;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestConfig", menuName = "Guild Manager/Quest Config")]
public class QuestConfig : ScriptableObject
{
    #region Variables
    [Header("Rank Configuration")]
    [Tooltip("One entry per rank, indexed by QuestRank (0 = F ... 7 = Special). " +
             "Must contain EXACTLY 8 entries. Created with defaults via Reset()")]
    [SerializeField] private RankConfig[] rankConfigs = new RankConfig[8];
    
    [Header("Category Base XP")]
    [Tooltip("Base adventurer XP for each quest category, BEFORE the rank multiplier is applied. " +
             "The final XP = categoryBaseXp × questRankXpMultiplier (configured per rank above). " +
             "Index matches QuestCategory enum order.")]
    [SerializeField] private int[] categoryBaseXp =
    {
        80,   // Combat
        50,   // Gathering
        70,   // Escort
        45,   // Delivery
        90,   // Subjugation
        65,   // Exploration
        60,   // Investigation
        120,  // Dungeon
    };
    
    [Header("Board Settings")]
    [Tooltip("Total quest slots on the guild board. Layout is always up to 2 rows of up to 5 slots.")]
    [SerializeField, Min(1)] private int maxBoardSlots = 10;
    
    [Header("Request Settings")]
    [Tooltip("How many new requests are drawn from the pool and made available each in-game day")]
    [SerializeField, Min(1)] private int requestsPerDay = 3;

    [Tooltip("After this many in-game days without being converted to a quest, a request is removed.")]
    [SerializeField, Min(1)] private int requestExpiryDays = 3;
    
    [Header("Application Window")]
    [Tooltip("Earliest hour (inclusive) at which adventurers will submit applications. Default 07:00.")]
    [SerializeField, Range(0, 23)] private int applicationWindowStartHour = 7;

    [Tooltip("Latest hour (inclusive) at which adventurers will submit applications. Default 16:00.")]
    [SerializeField, Range(0, 23)] private int applicationWindowEndHour = 16;
    
    [Header("Success Chance Formula")]
    [Tooltip("Floor on success chance regardless of how underpowered the party is.")]
    [SerializeField, Range(0f, 1f)] private float minSuccessChance = 0.05f;

    [Tooltip("Ceiling on success chance regardless of how overpowered the party is.")]
    [SerializeField, Range(0f, 1f)] private float maxSuccessChance = 0.95f;
    
    [Header("Completion Time Formula")]
    [Tooltip("Fastest a quest can resolve as a fraction of the remaining time at dispatch. " +
             "E.g. 0.2 means even the strongest party uses at least 20% of remaining time.")]
    [SerializeField, Range(0.05f, 0.5f)] private float minCompletionRatio = 0.20f;

    [Tooltip("Slowest a quest can resolve on success as a fraction of remaining time. " +
             "Should be below 1.0 so a successful party always returns before the deadline.")]
    [SerializeField, Range(0.5f, 0.99f)] private float maxCompletionRatio = 0.90f;

    [Tooltip("Random variance applied symmetrically to the computed completion time. " +
             "0.05 means ±5% of the computed duration.")]
    [SerializeField, Range(0f, 0.25f)] private float completionTimeVariance = 0.05f;
    #endregion
    
    #region Public Accessors
    public int MaxBoardSlots => maxBoardSlots;
    public int RequestsPerDay => requestsPerDay;
    public int RequestExpiryDays => requestExpiryDays;
    public int ApplicationWindowStartHour => applicationWindowStartHour;
    public int ApplicationWindowEndHour => applicationWindowEndHour;
    public float MinSuccessChance => minSuccessChance;
    public float MaxSuccessChance => maxSuccessChance;
    public float MinCompletionRatio => minCompletionRatio;
    public float MaxCompletionRatio => maxCompletionRatio;
    public float CompletionTimeVariance => completionTimeVariance;
    #endregion
    
    #region Rank Lookup
    // Returns the full RankConfig for a given rank. Logs an error if the array is malformed.
    public RankConfig GetRankConfig(QuestRank rank)
    {
        var index = (int)rank;
        if (rankConfigs == null || index < 0 || index >= rankConfigs.Length)
        {
            Debug.LogError($"[QuestConfig] No RankConfig at index {index} ({rank}). " +
                           $"Ensure the rankConfigs array has exactly 8 entries.");
            return default;
        }
        return rankConfigs[index];
    }
    
    // Returns the base XP for a quest category, used as the base of the XP formula:
    // finalXp = GetCategoryBaseXp(category) × (rankBaseXp / referenceRankXp).
    // Falls back to 50 if the category index is out of range.
    public int GetCategoryBaseXp(QuestCategory category)
    {
        var index = (int)category;
        if (categoryBaseXp == null || index < 0 || index >= categoryBaseXp.Length)
        {
            Debug.LogWarning($"[QuestConfig] No category base XP for {category}. Returning 50.");
            return 50;
        }
        return categoryBaseXp[index];
    }
    
    public string GetRankDisplayName(QuestRank rank) => GetRankConfig(rank).DisplayName;
    public Color GetRankColor(QuestRank rank) => GetRankConfig(rank).CardColor;
    public float GetRankPowerThreshold(QuestRank rank) => GetRankConfig(rank).BasePowerThreshold;
    public int GetRankBaseXp(QuestRank rank) => GetRankConfig(rank).BaseXpReward;
    #endregion
    
    #region Editor Default Initialization
    // Called by Unity when the asset is first created via the menu.
    // Pre-populates all 8 rank entries with the temp defaults, so the inspector is never left with blank entries.
    // All values remain fully editable afterward.
    private void Reset()
    {
        rankConfigs = new[]
        {
            new RankConfig("F", new Color(0.619f, 0.619f, 0.619f, 1f), 10f, 50, 2, 1, 0),
            new RankConfig("E", new Color(0.553f, 0.431f, 0.388f, 1f), 20f, 120, 3, 2, 1),
            new RankConfig("D", new Color(0.400f, 0.733f, 0.416f, 1f), 35f, 250, 5, 3, 1),
            new RankConfig("C", new Color(0.259f, 0.647f, 0.961f, 1f), 55f, 450, 8, 5, 2),
            new RankConfig("B", new Color(0.671f, 0.278f, 0.737f, 1f), 80f, 750, 12, 7, 3),
            new RankConfig("A", new Color(1.000f, 0.753f, 0.027f, 1f), 110f, 1200, 18, 10, 4),
            new RankConfig("S", new Color(0.937f, 0.325f, 0.314f, 1f), 150f, 2000, 25, 14, 5),
            new RankConfig("Special", new Color(0.149f, 0.776f, 0.855f, 1f), 200f, 5000, 35, 20, 7),
        };
        
        categoryBaseXp = new[]
        {
            80,
            50,
            70,
            45,
            90,
            65,
            60,
            120
        };
    }
    #endregion
    
    #region RankConfig Struct
    // Struct (not a class) so it is a value type stored inline in the array.
    // Serializable os Unity draws it in the inspector inside the QuestConfig asset.
    [Serializable]
    public struct RankConfig
    {
        [Tooltip("Letter or word shown in the UI for this rank. e.g. 'F', 'S', 'Special'.")]
        [SerializeField] private string displayName;
        
        [Tooltip("Background color used for quest cards of this rank.")]
        [SerializeField] private Color cardColor;
        
        [Tooltip("Expected combined party power for this rank. " +
                 "Used as the denominator in the success chance and completion time formulas. " +
                 "Plug adventurer stat totals in here once the adventurer system is built.")]
        [SerializeField, Min(1f)] private float basePowerThreshold;
        
        [Tooltip("Base experience points granted for completing a quest of this rank. " +
                 "The adventurer system will apply per-member scaling on top of this value.")]
        [SerializeField, Min(0)] private int baseXpReward;
        
        [Tooltip("Reputation gained by the guild when a quest of this rank completes successfully.")]
        public int reputationReward;
        
        [Tooltip("Reputation lost when a quest of this rank fails " +
                 "(party wipes, early failure, or in-progress deadline exceeded).")]
        public int reputationFailurePenalty;
        
        [Tooltip("Reputation lost when a quest of this rank expires on the board " +
                 "without any application ever being approved. " +
                 "Set to 0 to apply no penalty for unanswered jobs.")]
        public int reputationExpiryPenalty;
        
        // Constructor used by QuestConfig.Reset() to set defaults without reflection.
        public RankConfig(string name, Color color, float power, int xp, int reputationReward, int reputationFailurePenalty, int reputationExpiryPenalty)
        {
            displayName = name;
            cardColor = color;
            basePowerThreshold = power;
            baseXpReward = xp;
            this.reputationReward = reputationReward;
            this.reputationFailurePenalty = reputationFailurePenalty;
            this.reputationExpiryPenalty = reputationExpiryPenalty;
        }

        public string DisplayName => displayName;
        public Color CardColor => cardColor;
        public float BasePowerThreshold => basePowerThreshold;
        public int BaseXpReward => baseXpReward;
    }
    #endregion
}