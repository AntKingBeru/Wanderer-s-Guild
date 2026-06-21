// ScriptableObject holding every designer-tunable global number for the adventurer system:
// starting gold, the leveling cost curve, rank-up point thresholds, and random-arrival tuning.
// XpBracket struct lives in Structs.cs (Adventurer region).

using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "AdventurerConfig", menuName = "Guild Manager/Adventurer/Adventurer Config")]
public class AdventurerConfig : ScriptableObject
{
    #region Global Stats
    [Header("Global Stats")]
    [Tooltip("Gold every newly created adventurer starts with.")]
    [SerializeField, Min(0)] private int startingGold = 150;
    #endregion
    
    #region Leveling
    [Header("Leveling")]
    [Tooltip("Absolute maximum level any adventurer can reach.")]
    [SerializeField, Min(1)] private int maxLevel = 50;

    [Tooltip("XP cost brackets. For each bracket, XP to level up = xpPerLevelMultiplier × currentLevel. " +
             "Brackets must cover levels 1 through maxLevel with no gaps or overlaps.")]
    [SerializeField] private XpBracket[] levelUpThresholds = {
        new() { minLevel = 1, maxLevel = 10, xpPerLevelMultiplier = 100 },
        new() { minLevel = 11, maxLevel = 25, xpPerLevelMultiplier = 200 },
        new() { minLevel = 26, maxLevel = 50, xpPerLevelMultiplier = 400 },
    };
    #endregion
    
    #region Ranking
    [Header("Ranking")]
    [Tooltip("Rank points required to become eligible for a rank-up, indexed by QuestRank " +
             "(0=F→E … 6=S→Special). Index 7 (Special) is unused — it's the highest rank.")]
    [SerializeField] private int[] rankUpThresholds =
        { 500, 1500, 3500, 7500, 15000, 30000, 60000, 0 };
    #endregion
    
    #region Random Arrival Tuning
    [Header("Random Arrival — Timing")]
    [Tooltip("Minimum in-game days between spontaneous adventurer arrivals.")]
    [SerializeField, Min(0.1f)] private float arrivalRateMinDays = 2f;

    [Tooltip("Maximum in-game days between spontaneous adventurer arrivals.")]
    [SerializeField, Min(0.1f)] private float arrivalRateMaxDays = 3f;

    [Header("Random Arrival — Rank Distribution")]
    [Tooltip("Used ONLY when ProgressionSystem isn't in the scene (editor testing). " +
             "At runtime this is always overridden by ProgressionSystem.GuildRank.")]
    [SerializeField] private QuestRank guildRankCapFallback = QuestRank.C;

    [Tooltip("Relative probability weight per rank for newly arrived adventurers, indexed by " +
             "QuestRank (0=F … 7=Special). Normalised at runtime; ranks above the guild's current " +
             "rank are never rolled.")]
    [SerializeField] private float[] startingRankWeights = { 65f, 25f, 8f, 2f, 0f, 0f, 0f, 0f };

    [Header("Random Arrival — Level Range")]
    [Tooltip("How many levels of 'room' each rank gets when rolling a starting level. " +
             "E.g. 5 → rank F rolls levels 1-5, rank E rolls levels 6-10, etc.")]
    [SerializeField, Min(1)] private int levelsPerRank = 5;
    #endregion
    
    public int StartingGold => startingGold;
    public int MaxLevel => maxLevel;
    public float ArrivalRateMinDays => arrivalRateMinDays;
    public float ArrivalRateMaxDays => arrivalRateMaxDays;
    
    #region Computed Queries
    // XP required to advance FROM the given level to the next.
    public int GetXpForLevel(int level)
    {
        foreach (var bracket in levelUpThresholds)
            if (level >= bracket.minLevel && level <= bracket.maxLevel)
                return bracket.xpPerLevelMultiplier;

        Debug.LogWarning($"[AdventurerConfig] No XP bracket covers level {level}. Returning a safe fallback.");
        return int.MaxValue / 2;
    }

    // Rank points needed to become eligible to rank up FROM the given rank.
    public int GetRankPointThreshold(QuestRank rank)
    {
        var index = (int)rank;
        if (rankUpThresholds == null || index >= rankUpThresholds.Length)
        {
            Debug.LogWarning($"[AdventurerConfig] No rank-up threshold for {rank}. Returning 0.");
            return 0;
        }
        return rankUpThresholds[index];
    }

    // Draws a random starting rank using the configured weight distribution, capped at the
    // guild's current rank (or the editor fallback if ProgressionSystem isn't present).
    public QuestRank GetRandomStartingRank()
    {
        if (startingRankWeights == null || startingRankWeights.Length == 0)
            return QuestRank.F;

        var cap = ProgressionSystem.Instance
            ? (int)ProgressionSystem.Instance.MaxAdventurerArrivalRank
            : (int)guildRankCapFallback;

        var total = 0f;
        for (var i = 0; i <= cap && i < startingRankWeights.Length; i++)
            total += startingRankWeights[i];

        if (total <= 0f)
            return QuestRank.F;

        var roll = Random.Range(0f, total);
        var cumulative = 0f;
        for (var i = 0; i <= cap && i < startingRankWeights.Length; i++)
        {
            cumulative += startingRankWeights[i];
            if (roll <= cumulative)
                return (QuestRank)i;
        }
        return QuestRank.F;
    }

    // Rolls a random starting level appropriate for the given rank.
    public int GetRandomLevelForRank(QuestRank rank)
    {
        var rankIndex = (int)rank;
        var min = rankIndex * levelsPerRank + 1;
        var max = (rankIndex + 1) * levelsPerRank;
        return Mathf.Clamp(Random.Range(min, max + 1), 1, maxLevel);
    }
    #endregion
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (startingRankWeights != null && startingRankWeights.Length != 8)
            Debug.LogWarning("[AdventurerConfig] StartingRankWeights should have exactly 8 entries.");
        if (rankUpThresholds != null && rankUpThresholds.Length != 8)
            Debug.LogWarning("[AdventurerConfig] RankUpThresholds should have exactly 8 entries.");
        if (arrivalRateMinDays > arrivalRateMaxDays)
            Debug.LogWarning("[AdventurerConfig] ArrivalRateMinDays should not exceed ArrivalRateMaxDays.");
    }
#endif
}