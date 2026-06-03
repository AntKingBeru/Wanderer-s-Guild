// ScriptableObject that centralizes every designer-tunable constant in the adventurer system.
// XPBracket is a struct (not a class) and shares this file.

using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AdventurerConfig", menuName = "Guild Manager/Adventurer Config")]
public class AdventurerConfig : ScriptableObject
{
    #region Class Pool
    [Header("Class Pool")]
    [Tooltip("All available ClassData assets. The factory picks from this list with equal " +
             "probability between entries. Add new ClassData assets here when new classes are created.")]
    [SerializeField]
    private ClassData[] classPool;
    #endregion

    #region Leveling
    [Header("Leveling")] [Tooltip("Absolute maximum level any adventurer can reach.")] [SerializeField, Min(1)]
    private int maxLevel = 50;

    [Tooltip("XP cost brackets. For each bracket, XP to level up = xpPerLevelMultiplier × currentLevel. " +
             "Brackets must cover levels 1 through maxLevel with no gaps or overlaps.")]
    [SerializeField]
    private XpBracket[] xpBrackets = new XpBracket[]
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
    [SerializeField]
    private float[] rankPointThresholds =
        { 100f, 300f, 700f, 1500f, 3000f, 6000f, 12000f, 0f };

    [Tooltip("Power multiplier per rank, indexed by QuestRank (0=F … 7=Special). " +
             "Applied as: power = basePower × rankMultiplier.")]
    [SerializeField]
    private float[] rankMultipliers =
        { 1f, 1.5f, 2.25f, 3.5f, 5f, 7f, 10f, 15f };

    [Tooltip("The highest rank any adventurer in this guild can currently hold. " +
             "Set to C for the starting guild; updated when the guild rank system is added.")]
    [SerializeField]
    private QuestRank guildRankCap = QuestRank.C;
    #endregion

    #region PartySize
    [Header("Party Size")] [Tooltip("Maximum party size for standard rank adventurers.")] [SerializeField, Min(2)]
    private int normalMaxPartySize = 5;

    [Tooltip("Maximum party size when every member meets the elite rank threshold.")] [SerializeField, Min(2)]
    private int eliteMaxPartySize = 7;

    [Tooltip("Minimum rank each party member must hold to qualify for the elite size limit.")] [SerializeField]
    private QuestRank elitePartyMinRank = QuestRank.B;
    #endregion

    #region Maintenance
    [Header("Maintenance — Sleep")]
    [Tooltip("Success chance penalty (0–1) for missing exactly 1 consecutive night of sleep.")]
    [SerializeField, Range(0f, 0.5f)]
    private float sleepPenalty1Night = 0.05f;

    [Tooltip("Penalty for missing exactly 2 consecutive nights.")] [SerializeField, Range(0f, 0.5f)]
    private float sleepPenalty2Nights = 0.12f;

    [Tooltip("Penalty cap for missing 3 or more consecutive nights.")] [SerializeField, Range(0f, 0.5f)]
    private float sleepPenalty3PlusNights = 0.20f;

    [Header("Maintenance — Food")]
    [Tooltip("Days without food before any penalty begins (grace period).")]
    [SerializeField, Min(0)]
    private int foodGraceDays = 1;

    [Tooltip("Flat penalty per day without food beyond the grace period (0–1 scale).")]
    [SerializeField, Range(0f, 0.5f)]
    private float foodPenaltyPerDay = 0.08f;

    [Tooltip("Maximum cumulative food deprivation penalty regardless of duration.")] [SerializeField, Range(0f, 0.5f)]
    private float maxFoodPenalty = 0.20f;
    #endregion

    #region Early Quest Failure
    [Tooltip("Per-hour probability multiplier: chance = (1 − successChance) × this value. " +
             "Each check that passes adds one failure mark. Default: 0.08.")]
    [SerializeField, Range(0f, 0.5f)]
    private float failCheckChanceMultiplier = 0.08f;

    [Tooltip("Number of accumulated failure marks that triggers an immediate quest failure.")] [SerializeField, Min(1)]
    private int failMarksToFail = 3;
    #endregion

    #region Party Dynamics
    [Header("Party Dynamics")]
    [Tooltip("Rank gap between any two members (in ranks) that triggers a split probability check.")]
    [SerializeField, Min(1)]
    private int rankGapSplitThreshold = 2;

    [Tooltip("Probability of split when rank gap threshold is exceeded.")] [SerializeField, Range(0f, 1f)]
    private float rankGapSplitChance = 0.15f;

    [Tooltip("Split probability when exactly one party member dies.")] [SerializeField, Range(0f, 1f)]
    private float oneDeathSplitChance = 0.30f;

    [Tooltip("Split probability when two or more party members die.")] [SerializeField, Range(0f, 1f)]
    private float multiDeathSplitChance = 0.70f;

    [Tooltip("Consecutive failure count before the failure-streak split check begins.")] [SerializeField, Min(1)]
    private int consecutiveFailSplitThreshold = 3;

    [Tooltip("Additional split chance per consecutive failure beyond the threshold.")] [SerializeField, Range(0f, 1f)]
    private float consecutiveFailSplitChance = 0.25f;

    [Tooltip("HP fraction below which a member is considered critically low (e.g. 0.3 = 30%).")]
    [SerializeField, Range(0f, 1f)]
    private float lowHpThreshold = 0.30f;

    [Tooltip("Fraction of party members that must be critically low HP to trigger the morale check.")]
    [SerializeField, Range(0f, 1f)]
    private float lowHpMajorityFraction = 0.50f;

    [Tooltip("Split probability when the low-HP majority condition is met.")] [SerializeField, Range(0f, 1f)]
    private float lowHpSplitChance = 0.20f;

    [Tooltip("Quests a temporary party must complete together before being reclassified as permanent.")]
    [SerializeField, Min(1)]
    private int temporaryToPermQuestsRequired = 5;
    #endregion

    #region Rank-Up Quest
    [Header("Rank-Up Quest")]
    [Tooltip("Successful regular quests an adventurer must complete after a rank-up failure " +
             "before they can submit a new rank-up application.")]
    [SerializeField, Min(1)]
    private int rankUpRetryQuestsRequired = 5;

    [Tooltip("Cooldown in in-game months before a failed rank-up can be retried. " +
             "0.5 = half a month.")]
    [SerializeField, Min(0f)]
    private float rankUpRetryCooldownMonths = 0.5f;
    #endregion

    #region Adenturer Factory
    [Header("Adventurer Factory — Arrival Rate")]
    [Tooltip("Minimum in-game days between spontaneous adventurer arrivals. " +
             "Future systems (reputation, events) can modify the actual rate at runtime.")]
    [SerializeField, Min(0.5f)]
    private float arrivalMinDays = 2f;

    [Tooltip("Maximum in-game days between spontaneous adventurer arrivals.")] [SerializeField, Min(0.5f)]
    private float arrivalMaxDays = 3f;

    [Tooltip("Relative probability weight per rank for newly arrived adventurers, " +
             "indexed by QuestRank (0=F … 7=Special). Values are normalised at runtime " +
             "so they don't need to sum to 1.0 — raise or lower individual entries freely. " +
             "Ranks above GuildRankCap + MaxArrivalRankAboveCap are ignored.")]
    [SerializeField]
    private float[] startingRankWeights =
        { 0.65f, 0.25f, 0.08f, 0.02f, 0f, 0f, 0f, 0f };

    [Tooltip("How many ranks above the current guild cap a newly arrived adventurer can be. " +
             "Default 1: if guild cap is C, a B-rank adventurer can occasionally arrive. " +
             "Set to 0 to hard-cap arrivals at the guild's rank.")]
    [SerializeField, Min(0)]
    private int maxArrivalRankAboveCap = 1;
    #endregion

    #region Public Accessors
    public ClassData[] ClassPool => classPool;
    public int MaxLevel => maxLevel;
    public XpBracket[] XpBrackets => xpBrackets;
    public QuestRank GuildRankCap => guildRankCap;
    public int NormalMaxPartySize => normalMaxPartySize;
    public int EliteMaxPartySize => eliteMaxPartySize;
    public QuestRank ElitePartyMinRank => elitePartyMinRank;
    public int RankUpRetryQuestsRequired => rankUpRetryQuestsRequired;
    public float RankUpRetryCooldownMonths => rankUpRetryCooldownMonths;
    public float ArrivalMinDays => arrivalMinDays;
    public float ArrivalMaxDays => arrivalMaxDays;
    public int MaxArrivalRankAboveCap => maxArrivalRankAboveCap;
    public float FailCheckChanceMultiplier => failCheckChanceMultiplier;
    public int FailMarksToFail => failMarksToFail;
    public int RankGapSplitThreshold => rankGapSplitThreshold;
    public float RankGapSplitChance => rankGapSplitChance;
    public float OneDeathSplitChance => oneDeathSplitChance;
    public float MultiDeathSplitChance => multiDeathSplitChance;
    public int ConsecutiveFailSplitThreshold => consecutiveFailSplitThreshold;
    public float ConsecutiveFailSplitChance => consecutiveFailSplitChance;
    public float LowHpThreshold => lowHpThreshold;
    public float LowHpMajorityFraction => lowHpMajorityFraction;
    public float LowHpSplitChance => lowHpSplitChance;
    public int TemporaryToPermQuestsRequired => temporaryToPermQuestsRequired;
    public int FoodGraceDays => foodGraceDays;
    public float FoodPenaltyPerDay => foodPenaltyPerDay;
    public float MaxFoodPenalty => maxFoodPenalty;
    #endregion
    
    #region Lookup Methods
    // Power multiplier for a given rank. Returns 1 on invalid index rather than throw an exception.
    public float GetRankMultiplier(QuestRank rank)
    {
        var i = (int)rank;
        if (rankMultipliers == null || i >= rankMultipliers.Length)
        {
            Debug.LogError($"[AdventurerConfig] No rank multiplier for {rank}. Returning 1.");
            return 1f;
        }

        return rankMultipliers[i];
    }

    // Point threshold an adventurer at 'rank' must cross to become rank-up eligible.
    public float GetRankPointThreshold(QuestRank rank)
    {
        var i = (int)rank;
        if (rankPointThresholds == null || i >= rankPointThresholds.Length)
            return 0f;
        return rankPointThresholds[i];
    }

    // Xp required for an adventurer at 'level' to reach level +1.
    // Returns a safe fallback of 9999 if the level falls outside all brackets.
    public int GetXpRequiredForLevel(int level)
    {
        if (xpBrackets != null)
        {
            foreach (var bracket in xpBrackets)
                if (level >= bracket.minLevel && level <= bracket.maxLevel)
                    return bracket.xpPerLevelMultiplier * level;
        }

        Debug.LogWarning($"[AdventurerConfig] No XP bracket covers level {level}. Returning 9999.");
        return 9999;
    }

    // Returns the ClassData asset for the given class enum, or null if absent.
    public ClassData GetClassData(AdventurerClass adventurerClass)
    {
        return classPool?.FirstOrDefault(cd => cd && cd.AdventurerClass == adventurerClass);
    }

    // Flat success chance penalty for a given number of consecutive missed sleep nights.
    public float GetSleepPenalty(int missedNights)
    {
        return missedNights switch
        {
            <= 0 => 0f,
            1 => sleepPenalty1Night,
            2 => sleepPenalty2Nights,
            _ => sleepPenalty3PlusNights
        };
    }

    // Cumulative food deprivation penalty for the given number of days without food.
    public float GetFoodPenalty(int daysWithoutFood)
    {
        var penalisedDays = Mathf.Max(0, daysWithoutFood - foodGraceDays);
        return Mathf.Min(penalisedDays * foodPenaltyPerDay, maxFoodPenalty);
    }

    // Picks a random starting rank for a newly arrived adventurer.
    // Only weights for ranks ≤ guildRankCap + maxArrivalRankAboveCap are considered;
    // all others are ignored regardless of their weight values.
    // Normalizes the active weights so they don't need to sum to 1.0.
    public QuestRank GetRandomStartingRank()
    {
        if (startingRankWeights == null || startingRankWeights.Length == 0)
            return QuestRank.F;

        var maxIndex = Mathf.Clamp(
            (int)guildRankCap + maxArrivalRankAboveCap,
            0,
            startingRankWeights.Length - 1);

        var total = 0f;
        for (var i = 0; i <= maxIndex; i++)
            total += startingRankWeights[i];

        if (total <= 0f)
            return QuestRank.F;

        var roll = Random.value * total;
        var cumulative = 0f;
        for (var i = 0; i <= maxIndex; i++)
        {
            cumulative += startingRankWeights[i];
            if (roll <= cumulative)
                return (QuestRank)i;
        }

        // Floating-point overshoot fallback — return highest eligible rank.
        return (QuestRank)maxIndex;
    }
    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (arrivalMaxDays < arrivalMinDays)
        {
            arrivalMaxDays = arrivalMinDays;
            Debug.LogWarning("[AdventurerConfig] ArrivalMaxDays cannot be less than ArrivalMinDays. Clamped.");
        }

        if (rankPointThresholds != null && rankPointThresholds.Length != 8)
            Debug.LogWarning(
                "[AdventurerConfig] RankPointThresholds should have exactly 8 entries (one per QuestRank).");
        if (rankMultipliers != null && rankMultipliers.Length != 8)
            Debug.LogWarning("[AdventurerConfig] RankMultipliers should have exactly 8 entries.");
        if (startingRankWeights != null && startingRankWeights.Length != 8)
            Debug.LogWarning("[AdventurerConfig] StartingRankWeights should have exactly 8 entries.");
    }
#endif
}

// Struct — not a class. Defines an XP cost band over a range of levels.
// XP required to level up = xpPerLevelMultiplier × currentLevel.
[System.Serializable]
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