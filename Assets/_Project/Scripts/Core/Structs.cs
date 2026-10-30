using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    #region Scene Management
    // Maps a logical SceneId to the scene name listed in the Build Profile.
    [Serializable]
    public struct SceneEntry
    {
        [SerializeField] private SceneId id;
        [SerializeField] private string sceneName;

        public SceneEntry(SceneId id, string sceneName)
        {
            this.id = id;
            this.sceneName = sceneName;
        }
        
        public SceneId Id => id;
        public string SceneName => sceneName;
    }
    #endregion
    
    #region Bootstrap
    // One studio splash slide: what to show and its fade timings (seconds), unscaled).
    [Serializable]
    public struct SplashSlide
    {
        [Tooltip("Logo to display. Leave empty to show the placeholder text instead.")]
        [SerializeField] private Sprite logo;
        [SerializeField] private string placeholderText;
        [SerializeField, Min(0f)] private float delayBefore;
        [SerializeField, Min(0f)] private float fadeInDuration;
        [SerializeField, Min(0f)] private float holdDuration;
        [SerializeField, Min(0f)] private float fadeOutDuration;
        
        public SplashSlide(string placeholderText, float delayBefore, float fadeInDuration, float holdDuration, float fadeOutDuration)
        {
            logo = null;
            this.placeholderText = placeholderText;
            this.delayBefore = delayBefore;
            this.fadeInDuration = fadeInDuration;
            this.holdDuration = holdDuration;
            this.fadeOutDuration = fadeOutDuration;
        }

        public Sprite Logo => logo;
        public string PlaceholderText => placeholderText;
        public float DelayBefore => delayBefore;
        public float FadeInDuration => fadeInDuration;
        public float HoldDuration => holdDuration;
        public float FadeOutDuration => fadeOutDuration;
    }
    #endregion
    
    #region Audio
    // Maps a MusicTrackId to its clip and a per-track loudness balance.
    [Serializable]
    public struct MusicTrackEntry
    {
        [SerializeField] private MusicTrackId id;
        [Tooltip("Leave empty until the track exists; the game stays silent for this id.")]
        [SerializeField] private AudioClip clip;
        [Tooltip("Per-track loudness balance (0–1), applied before the mixer.")]
        [SerializeField, Range(0f, 1f)] private float volume;
        
        public MusicTrackEntry(MusicTrackId id, float volume)
        {
            this.id = id;
            clip = null;
            this.volume = volume;
        }

        public MusicTrackId Id => id;
        public AudioClip Clip => clip;
        public float Volume => volume;
    }
    
    // A UI sound: an optional clip plus its playback volume. A missing clip simply plays nothing.
    [Serializable]
    public struct UISoundClip
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume;

        public UISoundClip(float volume)
        {
            clip = null;
            this.volume = volume;
        }

        public AudioClip Clip => clip;
        public float Volume => volume;
    }
    #endregion
    
    #region MainMenu
    // One frame of a pixel-step animation: a whole-pixel offset from rest, shown for a duration (seconds).
    [Serializable]
    public struct PixelStep
    {
        [SerializeField] private Vector2Int offset;
        [SerializeField, Min(0.01f)] private float duration;

        public PixelStep(Vector2Int offset, float duration)
        {
            this.offset = offset;
            this.duration = duration;
        }

        public Vector2Int Offset => offset;
        public float Duration => duration;
    }
    #endregion
    
    #region Credits
    // One credits block: a heading (e.g. "Programming") and the names listed under it.
    [Serializable]
    public struct CreditsSection
    {
        [SerializeField] private string heading;
        [SerializeField] private string[] names;

        public string Heading => heading;

        // Never null, even for a section created with no names.
        public IReadOnlyList<string> Names => names ?? Array.Empty<string>();
    }
    #endregion
    
    #region Settings
    // The display settings that change the screen itself (and so need the keep prompt). Width/height 0 = native.
    public readonly struct ScreenModeSnapshot : IEquatable<ScreenModeSnapshot>
    {
        public ScreenModeSnapshot(int width, int height, WindowModeSetting windowMode, int monitorIndex)
        {
            Width = width;
            Height = height;
            WindowMode = windowMode;
            MonitorIndex = monitorIndex;
        }

        public int Width { get; }
        public int Height { get; }
        public WindowModeSetting WindowMode { get; }
        public int MonitorIndex { get; }

        public ScreenModeSnapshot WithResolution(int width, int height) => new(width, height, WindowMode, MonitorIndex);
        public ScreenModeSnapshot WithWindowMode(WindowModeSetting windowMode) => new(Width, Height, windowMode, MonitorIndex);
        public ScreenModeSnapshot WithMonitor(int monitorIndex) => new(Width, Height, WindowMode, monitorIndex);

        public bool Equals(ScreenModeSnapshot other) =>
            Width == other.Width && Height == other.Height && WindowMode == other.WindowMode && MonitorIndex == other.MonitorIndex;

        public override bool Equals(object obj) => obj is ScreenModeSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height, (int)WindowMode, MonitorIndex);
    }
    #endregion
    
    #region Time
    // An in-game calendar date (1-based). Calendar rules (month/season lengths) come with the time system.
    [Serializable]
    public struct GameDateStamp
    {
        [SerializeField] private int year;
        [SerializeField] private int month;
        [SerializeField] private int day;

        public GameDateStamp(int year, int month, int day)
        {
            this.year = year;
            this.month = month;
            this.day = day;
        }

        public int Year => year;
        public int Month => month;
        public int Day => day;
        public bool IsValid => year >= 1 && month >= 1 && day >= 1;
    }
    
    // One calendar month: its display name and the season it belongs to.
    [Serializable]
    public struct MonthDefinition
    {
        [SerializeField] private string name;
        [SerializeField] private Season season;

        public MonthDefinition(string name, Season season)
        {
            this.name = name;
            this.season = season;
        }

        public string Name => name;
        public Season Season => season;
    }
    
    // A moment of in-game time, fully broken down. TotalMinutes is the source of truth.
    public readonly struct GameDateTime : IEquatable<GameDateTime>
    {
        public GameDateTime(long totalMinutes, int year, int month, int day, int hour, int minute, Season season, DayPhase phase)
        {
            TotalMinutes = totalMinutes;
            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Season = season;
            Phase = phase;
        }

        // Minutes since Year 1, Month 1, Day 1, 00:00.
        public long TotalMinutes { get; }
        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        public int Hour { get; }
        public int Minute { get; }
        public Season Season { get; }
        public DayPhase Phase { get; }

        public GameDateStamp Date => new(Year, Month, Day);

        public bool Equals(GameDateTime other) => TotalMinutes == other.TotalMinutes;
        public override bool Equals(object obj) => obj is GameDateTime other && Equals(other);
        public override int GetHashCode() => TotalMinutes.GetHashCode();
    }
    #endregion
    
    #region Save Slots
    // One slot as read from disk. Header is null unless State is Ready.
    public readonly struct SaveSlotInfo
    {
        public SaveSlotInfo(int index, SaveSlotState state, SaveSlotHeader header)
        {
            Index = index;
            State = state;
            Header = header;
        }

        public int Index { get; }
        public SaveSlotState State { get; }
        public SaveSlotHeader Header { get; }
    }
    #endregion
    
    #region Intro
    // One cutscene slide: an image (or a placeholder color until art exists) and its narration line(s).
    [Serializable]
    public struct CutsceneSlide
    {
        [Tooltip("320×180 pixel-art image. Leave empty to show the placeholder colour.")]
        [SerializeField] private Sprite image;
        [SerializeField] private Color placeholderColor;

        [Tooltip("Plain text — press Enter in this field for a line break. Rich-text tags or a stray '<' would break the typewriter.")]
        [SerializeField, TextArea(3, 10)] private string text;

        public Sprite Image => image;
        public Color PlaceholderColor => placeholderColor;

        // NOTE: Pasted text may carry Windows line endings; the typewriter counts characters, so they become '\n'.
        public string Text => string.IsNullOrEmpty(text) ? string.Empty : text.Replace("\r\n", "\n").Replace('\r', '\n');
    }
    #endregion
    
    #region Guild
    // One guild rank's cumulative rank-point requirement and its display color.
    [Serializable]
    public struct RankDefinition
    {
        [SerializeField] private GuildRank rank;
        [SerializeField, Min(0)] private long pointsRequired;
        [SerializeField] private Color color;

        public RankDefinition(GuildRank rank, long pointsRequired, Color color)
        {
            this.rank = rank;
            this.pointsRequired = pointsRequired;
            this.color = color;
        }

        public GuildRank Rank => rank;
        public long PointsRequired => pointsRequired;
        public Color Color => color;
    }
    
    // One reputation level: the lowest value that counts as this level, and its display color.
    [Serializable]
    public struct ReputationLevelDefinition
    {
        [SerializeField] private ReputationLevel level;
        [SerializeField] private int minimumValue;
        [SerializeField] private Color color;

        public ReputationLevelDefinition(ReputationLevel level, int minimumValue, Color color)
        {
            this.level = level;
            this.minimumValue = minimumValue;
            this.color = color;
        }

        public ReputationLevel Level => level;
        public int MinimumValue => minimumValue;
        public Color Color => color;
    }
    #endregion
    
    #region Quests
    // One validated row of the Requests sheet, handed to RequestData.ApplyImport by the importer.
    public readonly struct RequestImportRow
    {
        public RequestImportRow(string id, string displayName, string objective, QuestCategory category, GuildRank recommendedRank,
            RequestSource source, int recommendedPartySize, RewardData reward, int expirationDays)
        {
            Id = id;
            DisplayName = displayName;
            Objective = objective;
            Category = category;
            RecommendedRank = recommendedRank;
            Source = source;
            RecommendedPartySize = recommendedPartySize;
            Reward = reward;
            ExpirationDays = expirationDays;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Objective { get; }
        public QuestCategory Category { get; }
        public GuildRank RecommendedRank { get; }
        public RequestSource Source { get; }
        public int RecommendedPartySize { get; }
        public RewardData Reward { get; }
        public int ExpirationDays { get; }
    }
    
    // What completing a request pays. Gold only for now. NOTE: Field name "gold" is used by the database tool.
    [Serializable]
    public struct RewardData
    {
        [SerializeField, Min(0)] private int gold;

        public RewardData(int gold) => this.gold = Mathf.Max(0, gold);

        public int Gold => gold;
    }

    // An open request as stored in game.json: which template, and when it was issued / expires (in-game minutes).
    [Serializable]
    public struct SavedRequest
    {
        [SerializeField] private int instanceId;
        [SerializeField] private string templateId;
        [SerializeField] private long issuedAtMinutes;
        [SerializeField] private long expiresAtMinutes;
        [SerializeField] private bool seen;

        public SavedRequest(int instanceId, string templateId, long issuedAtMinutes, long expiresAtMinutes, bool seen)
        {
            this.instanceId = instanceId;
            this.templateId = templateId;
            this.issuedAtMinutes = issuedAtMinutes;
            this.expiresAtMinutes = expiresAtMinutes;
            this.seen = seen;
        }

        public int InstanceId => instanceId;
        public string TemplateId => templateId;
        public long IssuedAtMinutes => issuedAtMinutes;
        public long ExpiresAtMinutes => expiresAtMinutes;
        public bool Seen => seen;
    }
    
    // The player's posting terms: share of the request's gold offered to the party, minimum adventurer rank, party size.
    public readonly struct QuestTerms
    {
        public QuestTerms(int rewardSharePercent, GuildRank minRank, int partySize)
        {
            RewardSharePercent = rewardSharePercent;
            MinRank = minRank;
            PartySize = partySize;
        }

        public int RewardSharePercent { get; }
        public GuildRank MinRank { get; }
        public int PartySize { get; }
    }
    
    // How much each stat counts toward party power for one quest category.
    [Serializable]
    public struct StatWeights
    {
        [SerializeField] private QuestCategory category;
        [SerializeField, Min(0f)] private float strength;
        [SerializeField, Min(0f)] private float agility;
        [SerializeField, Min(0f)] private float intellect;
        [SerializeField, Min(0f)] private float vitality;

        public StatWeights(QuestCategory category, float strength, float agility, float intellect, float vitality)
        {
            this.category = category;
            this.strength = strength;
            this.agility = agility;
            this.intellect = intellect;
            this.vitality = vitality;
        }

        public QuestCategory Category => category;

        public float Apply(StatBlock stats) =>
            stats.Strength * strength + stats.Agility * agility + stats.Intellect * intellect + stats.Vitality * vitality;
    }
    
    // A posted quest as stored in game.json (request by template id; members by adventurer id; decline cooldowns).
    [Serializable]
    public struct SavedQuest
    {
        [SerializeField] private int id;
        [SerializeField] private string requestId;
        [SerializeField] private long postedAtMinutes;
        [SerializeField] private long expiresAtMinutes;
        [SerializeField] private int rewardSharePercent;
        [SerializeField] private GuildRank minRank;
        [SerializeField] private int partySize;
        [SerializeField] private QuestState state;
        [SerializeField] private int[] memberIds;
        [SerializeField] private long departedAtMinutes;
        [SerializeField] private long returnsAtMinutes;
        [SerializeField] private int[] declinedIds;
        [SerializeField] private long[] declinedUntilMinutes;

        public SavedQuest(int id, string requestId, long postedAtMinutes, long expiresAtMinutes, QuestTerms terms,
                          QuestState state, int[] memberIds, long departedAtMinutes, long returnsAtMinutes,
                          int[] declinedIds, long[] declinedUntilMinutes)
        {
            this.id = id;
            this.requestId = requestId;
            this.postedAtMinutes = postedAtMinutes;
            this.expiresAtMinutes = expiresAtMinutes;
            rewardSharePercent = terms.RewardSharePercent;
            minRank = terms.MinRank;
            partySize = terms.PartySize;
            this.state = state;
            this.memberIds = memberIds;
            this.departedAtMinutes = departedAtMinutes;
            this.returnsAtMinutes = returnsAtMinutes;
            this.declinedIds = declinedIds;
            this.declinedUntilMinutes = declinedUntilMinutes;
        }

        public int Id => id;
        public string RequestId => requestId;
        public long PostedAtMinutes => postedAtMinutes;
        public long ExpiresAtMinutes => expiresAtMinutes;
        public QuestTerms Terms => new(rewardSharePercent, minRank, partySize);
        public QuestState State => state;
        public int[] MemberIds => memberIds ?? Array.Empty<int>();
        public long DepartedAtMinutes => departedAtMinutes;
        public long ReturnsAtMinutes => returnsAtMinutes;

        // Adventurers declined for this quest, and until when they can't re-apply (paired by index).
        public int[] DeclinedIds => declinedIds ?? Array.Empty<int>();
        public long[] DeclinedUntilMinutes => declinedUntilMinutes ?? Array.Empty<long>();
    }
    
    // One entry of the QuestCategoryIcons asset: a category and its 12×12 icon.
    [Serializable]
    public struct CategoryIconEntry
    {
        [SerializeField] private QuestCategory category;
        [SerializeField] private Sprite icon;

        public QuestCategory Category => category;
        public Sprite Icon => icon;
    }
    #endregion
    
    #region Adventurers
    // The four adventurer stats. Sheet format "Str,Agi,Int,Vit" (whole numbers).
    [Serializable]
    public struct StatBlock : IEquatable<StatBlock>
    {
        [SerializeField] private int strength;
        [SerializeField] private int agility;
        [SerializeField] private int intellect;
        [SerializeField] private int vitality;

        public StatBlock(int strength, int agility, int intellect, int vitality)
        {
            this.strength = strength;
            this.agility = agility;
            this.intellect = intellect;
            this.vitality = vitality;
        }

        public int Strength => strength;
        public int Agility => agility;
        public int Intellect => intellect;
        public int Vitality => vitality;
        public int Total => strength + agility + intellect + vitality;

        public static StatBlock operator +(StatBlock a, StatBlock b) =>
            new(a.strength + b.strength, a.agility + b.agility, a.intellect + b.intellect, a.vitality + b.vitality);

        public static StatBlock operator *(StatBlock a, int factor) =>
            new(a.strength * factor, a.agility * factor, a.intellect * factor, a.vitality * factor);

        // Each stat raised to at least <paramref name="minimum"/>.
        public StatBlock AtLeast(int minimum) =>
            new(Mathf.Max(minimum, strength), Mathf.Max(minimum, agility), Mathf.Max(minimum, intellect), Mathf.Max(minimum, vitality));

        public bool Equals(StatBlock other) =>
            strength == other.strength && agility == other.agility && intellect == other.intellect && vitality == other.vitality;

        public override bool Equals(object obj) => obj is StatBlock other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(strength, agility, intellect, vitality);
        public override string ToString() => $"STR {strength}  AGI {agility}  INT {intellect}  VIT {vitality}";
    }
    
    // An adventurer as stored in game.json (species and class by id; level comes from experience).
    [Serializable]
    public struct SavedAdventurer
    {
        [SerializeField] private int id;
        [SerializeField] private string name;
        [SerializeField] private string speciesId;
        [SerializeField] private string classId;
        [SerializeField] private long experience;
        [SerializeField] private GuildRank rank;
        [SerializeField] private int wealth;
        [SerializeField] private AdventurerStatus status;
        [SerializeField] private long joinedAtMinutes;
        [SerializeField] private StatBlock retainedGrowth;
        [SerializeField] private float rankProgress;
        [SerializeField] private long examReturnsAtMinutes;

        public SavedAdventurer(int id, string name, string speciesId, string classId, long experience, GuildRank rank,
                               int wealth, AdventurerStatus status, long joinedAtMinutes, StatBlock retainedGrowth,
                               float rankProgress, long examReturnsAtMinutes)
        {
            this.id = id;
            this.name = name;
            this.speciesId = speciesId;
            this.classId = classId;
            this.experience = experience;
            this.rank = rank;
            this.wealth = wealth;
            this.status = status;
            this.joinedAtMinutes = joinedAtMinutes;
            this.retainedGrowth = retainedGrowth;
            this.rankProgress = rankProgress;
            this.examReturnsAtMinutes = examReturnsAtMinutes;
        }

        public int Id => id;
        public string Name => name;
        public string SpeciesId => speciesId;
        public string ClassId => classId;
        public long Experience => experience;
        public GuildRank Rank => rank;
        public int Wealth => wealth;
        public AdventurerStatus Status => status;
        public long JoinedAtMinutes => joinedAtMinutes;
        public StatBlock RetainedGrowth => retainedGrowth;

        // Progress toward the next rank (0 in saves from before Session 13).
        public float RankProgress => rankProgress;

        // When a running promotion exam ends; 0 = no exam.
        public long ExamReturnsAtMinutes => examReturnsAtMinutes;
    }

    // One validated row of the Classes sheet, handed to ClassData.ApplyImport by the importer.
    public readonly struct ClassImportRow
    {
        public ClassImportRow(string id, string displayName, string description, StatBlock baseStats, StatBlock statGrowth,
                              GuildRank requiredGuildRank, string[] advancesToIds, QuestCategory[] affinities)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            BaseStats = baseStats;
            StatGrowth = statGrowth;
            RequiredGuildRank = requiredGuildRank;
            AdvancesToIds = advancesToIds;
            Affinities = affinities;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public StatBlock BaseStats { get; }
        public StatBlock StatGrowth { get; }
        public GuildRank RequiredGuildRank { get; }
        public string[] AdvancesToIds { get; }
        public QuestCategory[] Affinities { get; }

        // A copy with a different upgrade list (used after cross-row validation).
        public ClassImportRow WithAdvancesTo(string[] ids) =>
            new(Id, DisplayName, Description, BaseStats, StatGrowth, RequiredGuildRank, ids, Affinities);
    }

    // One validated row of the Species sheet, handed to SpeciesData.ApplyImport by the importer.
    public readonly struct SpeciesImportRow
    {
        public SpeciesImportRow(string id, string displayName, string description, StatBlock statModifiers)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            StatModifiers = statModifiers;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public StatBlock StatModifiers { get; }
    }
    
    // Whether an adventurer can take a promotion exam now: the target rank, its fee, or why not.
    public readonly struct PromotionCheck
    {
        private PromotionCheck(bool canPromote, bool hasTarget, GuildRank target, long fee, string reason)
        {
            CanPromote = canPromote;
            HasTarget = hasTarget;
            Target = target;
            Fee = fee;
            Reason = reason;
        }

        public bool CanPromote { get; }

        // False at the highest rank (no next rank to test for).
        public bool HasTarget { get; }
        public GuildRank Target { get; }
        public long Fee { get; }

        // Why not (empty when CanPromote).
        public string Reason { get; }

        public static PromotionCheck Eligible(GuildRank target, long fee) => new(true, true, target, fee, string.Empty);
        public static PromotionCheck Blocked(GuildRank target, long fee, string reason) => new(false, true, target, fee, reason);
        public static PromotionCheck HighestRank() => new(false, false, GuildRank.National, 0, "Highest rank");
    }
    #endregion
    
    #region Facilities
    // One validated row of the Facilities sheet, handed to FacilityData.ApplyImport by the importer.
    public readonly struct FacilityImportRow
    {
        public FacilityImportRow(string id, string displayName, string description, Vector2Int size, int maxLevel,
            long[] costs, int[] buildDays, GuildRank[] unlockRanks, int limit)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Size = size;
            MaxLevel = maxLevel;
            Costs = costs;
            BuildDays = buildDays;
            UnlockRanks = unlockRanks;
            Limit = limit;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Vector2Int Size { get; }
        public int MaxLevel { get; }
        public long[] Costs { get; }
        public int[] BuildDays { get; }
        public GuildRank[] UnlockRanks { get; }
        public int Limit { get; }
    }
    
    // A placed room as stored in game.json (facility by sheet id).
    [Serializable]
    public struct SavedFacility
    {
        [SerializeField] private int id;
        [SerializeField] private string facilityId;
        [SerializeField] private Vector2Int origin;
        [SerializeField] private int rotation;
        [SerializeField] private int level;
        [SerializeField] private int targetLevel;
        [SerializeField] private FacilityState state;
        [SerializeField] private long finishAtMinutes;
        [SerializeField] private long totalSpent;

        public SavedFacility(int id, string facilityId, Vector2Int origin, int rotation, int level, int targetLevel,
            FacilityState state, long finishAtMinutes, long totalSpent)
        {
            this.id = id;
            this.facilityId = facilityId;
            this.origin = origin;
            this.rotation = rotation;
            this.level = level;
            this.targetLevel = targetLevel;
            this.state = state;
            this.finishAtMinutes = finishAtMinutes;
            this.totalSpent = totalSpent;
        }

        public int Id => id;
        public string FacilityId => facilityId;
        public Vector2Int Origin => origin;
        public int Rotation => rotation;
        public int Level => level;
        public int TargetLevel => targetLevel;
        public FacilityState State => state;
        public long FinishAtMinutes => finishAtMinutes;
        public long TotalSpent => totalSpent;
    }
    
    // A connector in the room's unrotated footprint: its edge cell and which way the door faces.
    public readonly struct ConnectorSpec
    {
        public ConnectorSpec(Vector2Int cell, CardinalDirection facing)
        {
            Cell = cell;
            Facing = facing;
        }

        public Vector2Int Cell { get; }
        public CardinalDirection Facing { get; }
    }
    
    // Whether a build, upgrade or demolish is allowed now: its cost (or refund) and, if not, why.
    public readonly struct FacilityCheck
    {
        private FacilityCheck(bool allowed, long gold, string reason)
        {
            Allowed = allowed;
            Gold = gold;
            Reason = reason;
        }

        public bool Allowed { get; }

        // The cost (build / upgrade) or the refund (demolish).
        public long Gold { get; }
        public string Reason { get; }

        public static FacilityCheck Ok(long gold) => new(true, gold, string.Empty);
        public static FacilityCheck No(string reason, long gold = 0) => new(false, gold, reason);
    }
    #endregion
}