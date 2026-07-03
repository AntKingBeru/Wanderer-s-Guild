// Globally-accessible configuration hub (Singleton, execution order -101) for all systems.

using System;
using UnityEngine;

// Initializes before every other gameplay script
[DefaultExecutionOrder(-101)]
public class GameConfig : MonoSingleton<GameConfig>
{
    [Header("System Configuration Blocks")]
    [SerializeField] private TimeConfig time = new();
    [SerializeField] private EconomyConfig economy = new();
    [SerializeField] private ReputationConfig reputation = new();
    [SerializeField] private AdventurerConfig adventurer = new();
    [SerializeField] private QuestConfig quest = new();
    [SerializeField] private PartyConfig party = new();
    [SerializeField] private GuildConfig guild = new();
    [SerializeField] private ResolutionConfig resolution = new();
    [SerializeField] private WorldConfig world = new();
    
    public TimeConfig Time => time;
    public EconomyConfig Economy => economy;
    public ReputationConfig Reputation => reputation;
    public AdventurerConfig Adventurer => adventurer;
    public QuestConfig Quest => quest;
    public PartyConfig Party => party;
    public GuildConfig Guild => guild;
    public ResolutionConfig Resolution => resolution;
    public WorldConfig World => world;
    
    #region Time & Simulation
    [Serializable]
    public class TimeConfig
    {
        [Tooltip("Real seconds for one in-game day to pass at Normal speed.")]
        public float realSecondsPerDay = 90f;
        public float fastMultiplier = 3f;
        public float veryFastMultiplier = 8f;
        public int daysPerSeason = 30;
    }
    #endregion
    
    #region Economy
    [Serializable]
    public class EconomyConfig
    {
        public int startingGold = 500;
        public int dailyOperationCost = 25;
    }
    #endregion
    
    #region Repuatation
    [Serializable]
    public class ReputationConfig
    {
        public int startingReputation;
        public int minReputation = -100;
        public int maxReputation = 100;
        [Tooltip("Tier table asset mapping reputation bands to their effects.")]
        public ReputationTierTable tierTable;
    }
    #endregion
    
    #region Adventurer
    [Serializable]
    public class AdventurerConfig
    {
        [Header("Arrival Pacing")]
        public float baseArrivalIntervalDays = 3f;
        [Tooltip("Shortest arrival interval once reputation is high.")]
        public float minArrivalIntervalDays = 1f;
        [Tooltip("Reputation at which the arrival interval reaches its minimum.")]
        public int reputationForMinArrival = 200;
        [Tooltip("Max adventurers the guild can hold (later raised by Bedroom facilities).")]
        public int maxRosterSize = 20;

        [Header("Progression")]
        public int baseExperiencePerLevel = 100;
        [Tooltip("Hard cap; also constrained by current Guild Rank later.")]
        public GuildRank defaultRankCap = GuildRank.C;
    }
    #endregion
    
    #region Quest
    [Serializable]
    public class QuestConfig
    {
        [Range(0, 100)] public int defaultGuildRewardPercent = 30;
        public int baseRequestExpirationDays = 14;
        
        [Header("Request Generation")]
        [Tooltip("In-game days between requests at zero reputation.")]
        public float baseRequestIntervalDays = 2f;
        [Tooltip("Shortest interval once reputation is high.")]
        public float minRequestIntervalDays = 1f;
        [Tooltip("Reputation at which the interval reaches its minimum.")]
        public int reputationForMinInterval = 200;
        [Tooltip("Maximum simultaneously active requests on the board.")]
        public int maxActiveRequests = 12;
        [Tooltip("In-game days a posted quest stays on the board before expiring unfilled.")]
        public int postedQuestLifetimeDays = 10;
    }
    #endregion
    
    #region Party
    [Serializable]
    public class PartyConfig
    {
        public PartySizeRange lowRankSize = new(2, 5);
        public PartySizeRange highRankSize = new(3, 7);
    }
    #endregion
    
    #region Guild
    [Serializable]
    public class GuildConfig
    {
        [Header("Rank Progression")]
        [Tooltip("Guild-rank EXP granted per successful quest.")]
        public int rankExpPerQuestSuccess = 10;
        [Tooltip("EXP required to advance one guild rank.")]
        public int rankExpPerRank = 100;
        public GuildRank startingRank = GuildRank.F;
        [Tooltip("Quest-board slots = (int)GuildRank + this base. F→3 ... National→10.")]
        public int boardSlotBase = 3;
        public int MaxBoardSlots => (int)GuildRank.National + boardSlotBase;
    }
    #endregion
    
    #region Resolution
    [Serializable]
    public class ResolutionConfig
    {
        [Header("Stat Matching")]
        [Tooltip("Required party stat-total per difficulty tier (index = QuestDifficulty).")]
        public int[] difficultyStatThreshold = { 20, 45, 80, 130, 200, 300 };
        [Tooltip("Success chance when party total exactly meets the threshold.")]
        [Range(0f, 1f)] public float baseSuccessChance = 0.6f;
        [Tooltip("Success chance gained/lost per 10% over/under the threshold.")]
        [Range(0f, 1f)] public float chancePerTenPercent = 0.08f;

        [Header("Rewards")]
        public int experiencePerDifficulty = 40;
        public int rankProgressPerSuccess = 25;
        public int failureExperienceFraction = 25;

        [Header("Reputation Deltas")]
        public int reputationOnSuccess = 10;
        public int reputationOnFailure = -8;
        public int reputationOnDeath = -15;

        [Header("Casualty")]
        [Tooltip("Death chance per member on a failure, scaled by how badly the party fell short.")]
        [Range(0f, 1f)] public float baseDeathChanceOnFailure = 0.15f;
    }
    #endregion
    
    #region World
    [Serializable]
    public class WorldConfig
    {
        [Header("Movement")]
        public float agentSpeed = 3.5f;
        [Tooltip("Distance from destination counted as 'arrived'.")]
        public float arrivalTolerance = 0.4f;
        
        [Header("Reception Queue")]
        [Tooltip("Negative-Z spacing between adventurers lining up at the desk.")]
        public float queueSpacing = 1.2f;

        [Header("Patrol")]
        public float patrolRadius = 8f;
        public float minPatrolWaitSeconds = 2f;
        public float maxPatrolWaitSeconds = 6f;

        [Header("Doors")]
        public float doorOpenAngle = 90f;
        public float doorSpeed = 4f;
    }
    #endregion
}