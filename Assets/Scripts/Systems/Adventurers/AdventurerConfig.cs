// ScriptableObject that centralizes every designer-tunable constant in the adventurer system.
// XPBracket is a struct (not a class) and shares this file.

using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "AdventurerConfig", menuName = "Guild Manager/Adventurer Config")]
public class AdventurerConfig : ScriptableObject
{
    #region Class Pool
    [Header("Class Pool")]
    [Tooltip("All available ClassData assets. The factory picks from this list with equal " +
             "probability between entries. Add new ClassData assets here when new classes are created.")]
    [SerializeField] private ClassData[] classPool;
    #endregion

    #region Guild Rank Cap
    [Header("Guild Rank Cap — Editor Fallback")]
    [Tooltip("Used ONLY when ProgressionSystem is not present in the scene (editor testing). " +
             "At runtime this is always overridden by ProgressionSystem.GuildRank.")]
    [SerializeField] private QuestRank guildRankCapFallback = QuestRank.C;
    #endregion
    
    #region Power Formula
    [Header("Power Formula Weights")]
    [Tooltip("HP fraction in the power formula. HP + Damage + Speed weights should sum to 1.")]
    [SerializeField, Range(0f, 1f)] private float hpWeight = 0.3f;
    
    [Tooltip("Damage fraction in the power formula. HP + Damage + Speed weights should sum to 1.")]
    [SerializeField, Range(0f, 1f)] private float damageWeight = 0.5f;
    
    [Tooltip("Speed fraction in the power formula. HP + Damage + Speed weights should sum to 1.")]
    [SerializeField, Range(0f, 1f)] private float speedWeight = 0.2f;
    
    [Header("Rank Multipliers")]
    [Tooltip("Power multiplier per rank, indexed by QuestRank (0=F … 7=Special). " +
             "Applied as: power = basePower × rankMultiplier.")]
    [SerializeField] private float[] rankMultipliers =
        { 1f, 1.5f, 2.25f, 3.5f, 5f, 7f, 10f, 15f };
    #endregion
    
    #region Category Affinity Modifiers
    [Header("Category Affinity Modifiers")]
    [Tooltip("Flat success chance added per party member whose class is preferred for the quest category.")]
    [SerializeField, Range(0f, 0.5f)] private float preferredClassBonus = 0.08f;

    [Tooltip("Flat success chance removed per party member whose class is disliked for the quest category.")]
    [SerializeField, Range(0f, 0.5f)] private float dislikedClassPenalty = 0.12f;
    #endregion
    
    #region Leveling
    [Header("Leveling")]
    [Tooltip("Absolute maximum level any adventurer can reach.")]
    [SerializeField, Min(1)] private int maxLevel = 50;

    [Tooltip("XP cost brackets. For each bracket, XP to level up = xpPerLevelMultiplier × currentLevel. " +
             "Brackets must cover levels 1 through maxLevel with no gaps or overlaps.")]
    [SerializeField] private XpBracket[] xpBrackets = new XpBracket[]
    {
        new XpBracket { minLevel = 1, maxLevel = 10, xpPerLevelMultiplier = 100 },
        new XpBracket { minLevel = 11, maxLevel = 25, xpPerLevelMultiplier = 200 },
        new XpBracket { minLevel = 26, maxLevel = 50, xpPerLevelMultiplier = 400 },
    };
    #endregion
    
    #region Ranking
    [Header("Ranking")]
    [Tooltip("Rank point threshold to become eligible for a rank-up quest, indexed by " +
             "QuestRank (0=F→E … 6=S→Special). The last entry (Special) is unused " +
             "since Special is the highest rank.")]
    [SerializeField] private int[] rankPointThresholds =
        { 100, 300, 700, 1500, 3000, 6000, 12000, 0 };
    #endregion
    
    #region Adenturer Factory
    [Header("Adventurer Factory — Arrival Rate")]
    [Tooltip("Minimum in-game days between spontaneous adventurer arrivals. " +
             "Future systems (reputation, events) can modify the actual rate at runtime.")]
    [SerializeField, Min(0.5f)] private float arrivalRateMinDays = 2f;

    [Tooltip("Maximum in-game days between spontaneous adventurer arrivals.")]
    [SerializeField, Min(0.5f)] private float arrivalRateMaxDays = 3f;

    [Tooltip("Relative probability weight per rank for newly arrived adventurers, " +
             "indexed by QuestRank (0=F … 7=Special). Values are normalised at runtime " +
             "so they don't need to sum to 1.0 — raise or lower individual entries freely. " +
             "Ranks above GuildRankCap + MaxArrivalRankAboveCap are ignored.")]
    [SerializeField] private float[] startingRankWeights =
        { 65f, 25f, 8f, 2f, 0f, 0f, 0f, 0f };
    
    [Tooltip("Base probability per in-game hour that an idle adventurer submits a quest application.")]
    [SerializeField, Range(0f, 1f)] private float baseApplicationChancePerHour = 0.15f;
    
    [Tooltip("Application rate multiplier per season. Index matches Season enum (0=Spring…3=Winter).")]
    [SerializeField] private float[] seasonApplicationRateModifiers = { 1.0f, 0.8f, 1.2f, 0.6f };
    #endregion
    
    #region Maintenance
    [Header("Maintenance — Sleep")]
    [Tooltip("Success chance penalty (0–1) for missing exactly 1 consecutive night of sleep.")]
    [SerializeField, Range(0f, 0.5f)] private float[] sleepMissedPenalties =
        { 0f, 0.05f, 0.12f, 0.2f };

    [Header("Maintenance — Food")]
    [Tooltip("Days without food before any penalty begins (grace period).")]
    [SerializeField, Min(0)]
    private float[] foodDeprivedPenalties = 
        { 0f, 0f, 0.08f, 0.2f };
    #endregion

    #region Early Quest Failure
    [Tooltip("Per-hour probability multiplier: chance = (1 − successChance) × this value. " +
             "Each check that passes adds one failure mark. Default: 0.08.")]
    [SerializeField, Range(0f, 0.5f)] private float earlyFailureCoefficient = 0.08f;

    [Tooltip("Number of accumulated failure marks that triggers an immediate quest failure.")]
    [SerializeField, Min(1)] private int earlyFailureMarksRequired = 3;
    #endregion

    #region Rank-Up Quest
    [Header("Rank-Up Quest")]
    [Tooltip("Successful regular quests an adventurer must complete after a rank-up failure " +
             "before they can submit a new rank-up application.")]
    [SerializeField, Min(1)] private int rankUpRetrySuccessesRequired = 5;

    [Tooltip("Cooldown in in-game months before a failed rank-up can be retried. " +
             "0.5 = half a month.")]
    [SerializeField, Min(0f)] private float rankUpRetryCooldownMonthFraction = 0.5f;
    #endregion
    
    #region Party
    [Header("Party Size And Limits")]
    [Tooltip("Maximum party size when any member is below the high-threshold.")]
    [SerializeField, Range(2, 7)] private int normalPartyMaxSize = 5;
    
    [Tooltip("Maximum party size when ALL members are at or above the high-threshold.")]
    [SerializeField, Range(2, 7)] private int highRankPartyMaxSize = 7;
    
    [Tooltip("Rank at which the expanded party size becomes available. " +
             "All members must meet or exceed this rank.")]
    [SerializeField] private QuestRank highRankPartyThreshold = QuestRank.A;
    
    [Tooltip("Number of quests a temporary party must complete together before it can convert to a registered permanent party.")]
    [SerializeField, Min(1)] private int temporaryPartyQuestsToMakePermanent = 5;
    
    [Header("Party Deterioration")]
    [Tooltip("Rank gap in steps between any two members that triggers a split probability check.")]
    [SerializeField, Min(1)] private int rankGapSplitThreshold = 2;
    
    [Tooltip("Probability of a split event when rank gap exceeds the threshold.")]
    [SerializeField, Range(0f, 1f)] private float rankGapSplitChance = 0.15f;
    
    [Tooltip("Probability of a split event when exactly one member dies.")]
    [SerializeField, Range(0f, 1f)] private float oneMemberDeathSplitChance = 0.3f;
    
    [Tooltip("Probability of a full disband when two or more members die.")]
    [SerializeField, Range(0f, 1f)] private float multiMemberDeathDisbandChance = 0.7f;
    
    [Tooltip("Number of consecutive failures before each additional failure triggers a split event.")]
    [SerializeField, Min(1)] private int consecutiveFailSplitThreshold = 3;
    
    [Tooltip("Split probability added per consecutive failure beyond the threshold.")]
    [SerializeField, Range(0f, 1f)] private float consecutiveFailSplitChancePerExtra = 0.25f;
    
    [Tooltip("HP fraction below which a member is considered critically low. " +
             "0.3 = below 30% of their max HP.")]
    [SerializeField, Range(0f, 1f)] private float lowHpThreshold = 0.3f;
    
    [Tooltip("Minimum number of members simultaneously at critically low HP to trigger a split check.")]
    [SerializeField, Min(1)] private int lowHpMemberCountForMoraleCheck = 2;
    
    [Tooltip("Probability of a split event when the low-HP morale check triggers.")]
    [SerializeField, Range(0f, 1f)] private float lowHpMoraleSplitChance = 0.2f;
    #endregion

    #region Public Accessors
    public ClassData[] ClassPool => classPool;
    public QuestRank GuildRankCap
        => ProgressionSystem.Instance
            ? ProgressionSystem.Instance.GuildRank
            : guildRankCapFallback;
    public float HpWeight => hpWeight;
    public float DamageWeight => damageWeight;
    public float SpeedWeight => speedWeight;
    public float PreferredClassBonus => preferredClassBonus;
    public float DislikedClassPenalty => dislikedClassPenalty;
    public int MaxLevel => maxLevel;
    public float ArrivalRateMinDays => arrivalRateMinDays;
    public float ArrivalRateMaxDays => arrivalRateMaxDays;
    public float BaseApplicationChancePerHour => baseApplicationChancePerHour;
    public float EarlyFailureCoefficient => earlyFailureCoefficient;
    public int EarlyFailureMarksRequired => earlyFailureMarksRequired;
    public int RankUpRetrySuccessesRequired => rankUpRetrySuccessesRequired;
    public float RankUpRetryCooldownMonthFraction => rankUpRetryCooldownMonthFraction;
    public int NormalPartyMaxSize => normalPartyMaxSize;
    public int HighRankPartyMaxSize => highRankPartyMaxSize;
    public QuestRank HighRankPartyThreshold => highRankPartyThreshold;
    public int TemporaryPartyQuestsToMakePermanent => temporaryPartyQuestsToMakePermanent;
    public int RankGapSplitThreshold => rankGapSplitThreshold;
    public float RankGapSplitChance => rankGapSplitChance;
    public float OneMemberDeathSplitChance => oneMemberDeathSplitChance;
    public float MultiMemberDeathDisbandChance => multiMemberDeathDisbandChance;
    public int ConsecutiveFailSplitThreshold => consecutiveFailSplitThreshold;
    public float ConsecutiveFailSplitChancePerExtra => consecutiveFailSplitChancePerExtra;
    public float LowHpThreshold => lowHpThreshold;
    public int LowHpMemberCountForMoraleCheck => lowHpMemberCountForMoraleCheck;
    public float LowHpMoraleSplitChance => lowHpMoraleSplitChance;
    #endregion
    
    #region Computed Queries
    // Returns XP required to advance FROM the given level to the next.
    // GetXpForLevel(1) = XP needed to reach level 2.
    public int GetXpForLevel(int level)
        => xpBrackets.First(b => b.minLevel <= level).xpPerLevelMultiplier;
    
    // Returns the power rank multiplier for a given rank.
    public float GetRankMultiplier(QuestRank rank)
    {
        var index = (int)rank;
        if (rankMultipliers == null || index >= rankMultipliers.Length)
        {
            Debug.LogWarning($"[AdventurerConfig] No rank multiplier for {rank}. Return 1.");
            return 1f;
        }
        return rankMultipliers[index];
    }
    
    // Returns the rank point threshold an adventurer must cross to unlock the rank-up quest FROM the given rank.
    public int GetRankPointThreshold(QuestRank rank)
    {
        var index = (int)rank;
        if (rankPointThresholds == null || index >= rankPointThresholds.Length)
        {
            Debug.LogWarning($"[AdventurerConfig] No rank point threshold for {rank}. Return 0.");
            return 0;
        }
        return rankPointThresholds[index];
    }
    
    // Returns the maintenance success chance penalty for the given amount of missed sleep night.
    // nightsMissed = 0 always returns 0 (no penalty). Values beyond the array clamp to the last entry.
    public float GetSleepMissedPenalty(int nightsMissed)
    {
        if (sleepMissedPenalties == null || sleepMissedPenalties.Length == 0)
            return 0f;
        return sleepMissedPenalties[Mathf.Clamp(nightsMissed, 0, sleepMissedPenalties.Length - 1)];
    }
    
    // Returns the maintenance success chance penalty for the given number of days without food.
    // daysDeprived = 0 always returns 0 (no penalty). Values beyond the array clamp to the last entry.
    public float GetFoodDeprivedPenalty(int daysDeprived)
    {
        if (foodDeprivedPenalties == null || foodDeprivedPenalties.Length == 0)
            return 0f;
        return foodDeprivedPenalties[Mathf.Clamp(daysDeprived, 0, foodDeprivedPenalties.Length - 1)];
    }

    public float GetSeasonApplicationModifier(Season season)
    {
        var index = (int)season;
        if (seasonApplicationRateModifiers == null || index >= seasonApplicationRateModifiers.Length)
            return 1f;
        return seasonApplicationRateModifiers[index];
    }
    
    // Draws a random starting rank using the configured weight distribution.
    // Weights are normalized internally and do not need to sum to any specific total.
    // The result is clamped to one rank above guildRankCap.
    public QuestRank GetRandomStartingRank()
    {
        if (startingRankWeights == null || startingRankWeights.Length == 0)
            return QuestRank.F;

        var total = 0f;
        // Only sum weights up to and including the allowed arrival cap.
        var arrivalCap = ProgressionSystem.Instance
            ? (int)ProgressionSystem.Instance.MaxAdventurerArrivalRank
            : (int)guildRankCapFallback;

        for (var i = 0; i <= arrivalCap && i < startingRankWeights.Length; i++)
            total += startingRankWeights[i];

        if (total <= 0f)
            return QuestRank.F;

        var roll = Random.Range(0f, total);
        var cumulative = 0f;
        for (var i = 0; i <= arrivalCap && i < startingRankWeights.Length; i++)
        {
            cumulative += startingRankWeights[i];
            if (roll <= cumulative)
                return (QuestRank)i;
        }
        return QuestRank.F;
    }
    
    // Returns the ClassData asset for the given class enum, or null if absent from the pool.
    public ClassData GetClassData(AdventurerClass adventurerClass)
    {
        if (classPool == null)
            return null;
        return classPool.FirstOrDefault(cd => cd && cd.AdventurerClass == adventurerClass);
    }
    public ClassData GetRandomClassData()
    {
        if (classPool == null || classPool.Length == 0)
        {
            Debug.LogWarning("[AdventurerConfig] Class pool is empty. Cannot pick a random class.");
            return null;
        }
        return classPool[Random.Range(0, classPool.Length)];
    }
    
    // Returns the party size cap given the lowest rank among all party members.
    // All members must be at or above the highRankPartyThreshold for the expanded limit.
    public int GetPartyMaxSize(QuestRank lowestMemberRank) 
        => (int)lowestMemberRank >= (int)highRankPartyThreshold 
            ? highRankPartyMaxSize 
            : normalPartyMaxSize;
    #endregion

#if UNITY_EDITOR
    private void Reset()
    {
        rankMultipliers = new[] { 1f, 1.5f, 2.25f, 3.5f, 5f, 7f, 10f, 15f };
        startingRankWeights = new[] { 65f, 25f, 8f, 2f, 0f, 0f, 0f, 0f };
        rankPointThresholds = new[] { 100, 300, 700, 1500, 3000, 6000, 12000, 0 };
        sleepMissedPenalties = new[] { 0f, 0.05f, 0.12f, 0.2f };
        foodDeprivedPenalties = new[] { 0f, 0f, 0.08f, 0.2f };
        xpBrackets = new[]
        {
            new XpBracket { minLevel = 1, maxLevel = 10, xpPerLevelMultiplier = 100 },
            new XpBracket { minLevel = 11, maxLevel = 25, xpPerLevelMultiplier = 200 },
            new XpBracket { minLevel = 26, maxLevel = 50, xpPerLevelMultiplier = 400 }
        };
    }
    private void OnValidate()
    {
        if (rankMultipliers != null && rankMultipliers.Length != 8)
            Debug.LogWarning("[AdventurerConfig] RankMultipliers should have exactly 8 entries.");
        if (startingRankWeights != null && startingRankWeights.Length != 8)
            Debug.LogWarning("[AdventurerConfig] StartingRankWeights should have exactly 8 entries.");
        if (rankPointThresholds != null && rankPointThresholds.Length != 8)
            Debug.LogWarning("[AdventurerConfig] RankPointThresholds should have exactly 8 entries.");
        if (arrivalRateMinDays > arrivalRateMaxDays)
            Debug.LogWarning("[AdventurerConfig] ArrivalRateMinDays should not exceed ArrivalRateMaxDays.");
        if (normalPartyMaxSize > highRankPartyMaxSize)
            Debug.LogWarning("[AdventurerConfig] NormalPartyMaxSize should not exceed HighRankPartyMaxSize.");
        var weightSum = hpWeight + damageWeight + speedWeight;
        if (Mathf.Abs(weightSum - 1f) > 0.01f)
            Debug.LogWarning($"[AdventurerConfig] Power formula weight sum to {weightSum:F2}; " +
                             "they should sum to 1.0 for a balanced formula.");
    }
#endif
}

// Struct — not a class. Defines an XP cost band over a range of levels.
// XP required to level up = xpPerLevelMultiplier × currentLevel.
[Serializable]
public struct XpBracket
{
    [Tooltip("First level in this bracket (inclusive).")]
    [Min(1)] public int minLevel;

    [Tooltip("Last level in this bracket (inclusive).")]
    [Min(1)] public int maxLevel;

    [Tooltip("XP to level up = this value × the adventurer's current level. " +
             "E.g. multiplier 100 at level 5 costs 500 XP to reach level 6.")]
    [Min(1)] public int xpPerLevelMultiplier;
}