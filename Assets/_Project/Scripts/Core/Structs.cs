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
    
    #region Lighting
    // The sun's look at one moment of the day (from DayNightCycle).
    public readonly struct SunState
    {
        public SunState(Quaternion rotation, Color color, float intensity, Color ambient, float daylight)
        {
            Rotation = rotation;
            Color = color;
            Intensity = intensity;
            Ambient = ambient;
            Daylight = daylight;
        }

        public Quaternion Rotation { get; }
        public Color Color { get; }
        public float Intensity { get; }
        public Color Ambient { get; }

        // 0 = deepest night … 1 = brightest noon (for interior lights later).
        public float Daylight { get; }
    }
    #endregion
    
    #region Save Slots
    // One slot as read from disk. Header == null unless State is Ready.
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
    
    // A posted quest as stored in game.json (request by template id; members by adventurer id; decline cooldowns;
    // potions taken at departure).
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
        [SerializeField] private int potionTier;
        [SerializeField] private int potionShareCut;
        [SerializeField] private float potionChanceBonus;

        public SavedQuest(int id, string requestId, long postedAtMinutes, long expiresAtMinutes, QuestTerms terms,
                          QuestState state, int[] memberIds, long departedAtMinutes, long returnsAtMinutes,
                          int[] declinedIds, long[] declinedUntilMinutes, int potionTier, int potionShareCut, float potionChanceBonus)
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
            this.potionTier = potionTier;
            this.potionShareCut = potionShareCut;
            this.potionChanceBonus = potionChanceBonus;
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

        // Potions taken at departure (0 = none).
        public int PotionTier => potionTier;
        public int PotionShareCut => potionShareCut;
        public float PotionChanceBonus => potionChanceBonus;
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
    // One adventurer a new guild starts with (GameConfig › Adventurers › Starting Party). Empty name = random.
    [Serializable]
    public struct StartingAdventurerSpec
    {
        [SerializeField] private SpeciesData species;
        [SerializeField] private ClassData classData;
        [SerializeField, Min(1)] private int level;
        [Tooltip("Optional fixed name; empty = a random one.")]
        [SerializeField] private string name;

        public SpeciesData Species => species;
        public ClassData Class => classData;
        public int Level => Mathf.Max(1, level);
        public string Name => name;
        public bool IsValid => species != null && classData != null;
    }
    
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
    
    // An adventurer as stored in game.json (species, class and gear by id; level comes from experience).
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
        [SerializeField] private string[] equipmentIds;
        [SerializeField] private float wellFedBonus;
        [SerializeField] private InjurySeverity injury;
        [SerializeField] private long injuredUntilMinutes;
        [SerializeField] private long returningUntilMinutes;
        [SerializeField] private string trialClassId;
        [SerializeField] private long trialReturnsAtMinutes;

        public SavedAdventurer(int id, string name, string speciesId, string classId, long experience, GuildRank rank,
                               int wealth, AdventurerStatus status, long joinedAtMinutes, StatBlock retainedGrowth,
                               float rankProgress, long examReturnsAtMinutes, string[] equipmentIds, float wellFedBonus,
                               InjurySeverity injury, long injuredUntilMinutes, long returningUntilMinutes,
                               string trialClassId, long trialReturnsAtMinutes)
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
            this.equipmentIds = equipmentIds;
            this.wellFedBonus = wellFedBonus;
            this.injury = injury;
            this.injuredUntilMinutes = injuredUntilMinutes;
            this.returningUntilMinutes = returningUntilMinutes;
            this.trialClassId = trialClassId;
            this.trialReturnsAtMinutes = trialReturnsAtMinutes;
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

        // Progress toward the next rank.
        public float RankProgress => rankProgress;

        // When a running promotion exam ends; 0 = no exam.
        public long ExamReturnsAtMinutes => examReturnsAtMinutes;

        // Item ids by EquipmentSlot index (empty / null entries = nothing worn).
        public string[] EquipmentIds => equipmentIds ?? Array.Empty<string>();
        
        // The supper bonus they're carrying (0 = not well-fed)
        public float WellFedBonus => wellFedBonus;
        
        public InjurySeverity Injury => injury;
        public long InjuredUntilMinutes => injuredUntilMinutes;
        
        // When a recalled adventurer gets back (0 = not returning; 0 in saves from before Session 22).
        public long ReturningUntilMinutes => returningUntilMinutes;
        
        // A running class trial: the class they're trying for and when they're back (empty / 0 = none).
        public string TrialClassId => trialClassId;
        public long TrialReturnsAtMinutes => trialReturnsAtMinutes;
        
    }

    // One validated row of the Classes sheet, handed to ClassData.ApplyImport by the importer.
    public readonly struct ClassImportRow
    {
        public ClassImportRow(string id, string displayName, string description, StatBlock baseStats, StatBlock statGrowth,
            GuildRank requiredGuildRank, GuildRank maxRank, string[] advancesToIds, QuestCategory[] affinities,
            string[] startingGearIds)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            BaseStats = baseStats;
            StatGrowth = statGrowth;
            RequiredGuildRank = requiredGuildRank;
            MaxRank = maxRank;
            AdvancesToIds = advancesToIds;
            Affinities = affinities;
            StartingGearIds = startingGearIds;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public StatBlock BaseStats { get; }
        public StatBlock StatGrowth { get; }
        public GuildRank RequiredGuildRank { get; }
        public GuildRank MaxRank { get; }
        public string[] AdvancesToIds { get; }
        public QuestCategory[] Affinities { get; }
        public string[] StartingGearIds { get; }

        // A copy with a different upgrade list (used after cross-row validation).
        public ClassImportRow WithAdvancesTo(string[] ids) =>
            new(Id, DisplayName, Description, BaseStats, StatGrowth, RequiredGuildRank, MaxRank, ids, Affinities, StartingGearIds);
    }

    // One validated row of the Species sheet, handed to SpeciesData.ApplyImport by the importer.
    public readonly struct SpeciesImportRow
    {
        public SpeciesImportRow(string id, string displayName, string description, StatBlock statModifiers, StatBlock growth)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            StatModifiers = statModifiers;
            Growth = growth;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public StatBlock StatModifiers { get; }
        public StatBlock Growth { get; }
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
    
    // A waiting applicant as stored in game.json: the adventurer they'd become, and when they leave.
    [Serializable]
    public struct SavedApplicant
    {
        [SerializeField] private SavedAdventurer adventurer;
        [SerializeField] private long expiresAtMinutes;

        public SavedApplicant(SavedAdventurer adventurer, long expiresAtMinutes)
        {
            this.adventurer = adventurer;
            this.expiresAtMinutes = expiresAtMinutes;
        }

        public SavedAdventurer Adventurer => adventurer;
        public long ExpiresAtMinutes => expiresAtMinutes;
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
    
    // A door in the room's unrotated footprint: its first cell (lower x for north/south doors, lower z for
    // east/west doors — the second cell is the next one along the edge) and which way it faces.
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
    
    // A 2-cell-wide door on the grid: both cells (kept in a fixed order, so equal doors compare equal at any
    // rotation) and its facing.
    public readonly struct WorldDoor : IEquatable<WorldDoor>
    {
        public WorldDoor(Vector2Int a, Vector2Int b, CardinalDirection facing)
        {
            var swap = b.x < a.x || (b.x == a.x && b.y < a.y);
            A = swap ? b : a;
            B = swap ? a : b;
            Facing = facing;
        }

        public Vector2Int A { get; }
        public Vector2Int B { get; }
        public CardinalDirection Facing { get; }

        // The door that would meet this one: both cells just outside it, facing back.
        public WorldDoor Partner
        {
            get
            {
                var step = FacilityFootprint.Step(Facing);
                return new WorldDoor(A + step, B + step, FacilityFootprint.Opposite(Facing));
            }
        }

        public bool Equals(WorldDoor other) => A == other.A && B == other.B && Facing == other.Facing;
        public override bool Equals(object obj) => obj is WorldDoor other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(A, B, (int)Facing);
    }
    
    // One card of the build catalog: the facility, why it can't be chosen (empty = selectable), and affordability.<
    public readonly struct BuildCatalogEntry
    {
        public BuildCatalogEntry(FacilityData data, string lockReason, bool affordable)
        {
            Data = data;
            LockReason = lockReason ?? string.Empty;
            Affordable = affordable;
        }

        public FacilityData Data { get; }
        public string LockReason { get; }
        public bool IsLocked => LockReason.Length > 0;
        public bool Affordable { get; }
    }
    
    // One validated row of the equipment sheet, handed to EquipmentData.ApplyImport by the importer
    public readonly struct EquipmentImportRow
    {
        public EquipmentImportRow(string id, string displayName, string description, EquipmentSlot slot, string[] classIds,
            StatBlock stats, int price, int armoryLevel, GuildRank unlockRank, bool twoHanded, int stock)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Slot = slot;
            ClassIds = classIds;
            Stats = stats;
            Price = price;
            ArmoryLevel = armoryLevel;
            UnlockRank = unlockRank;
            TwoHanded = twoHanded;
            Stock = stock;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public EquipmentSlot Slot { get; }
        public string[] ClassIds { get; }
        public StatBlock Stats { get; }
        public int Price { get; }
        public int ArmoryLevel { get; }
        public GuildRank UnlockRank { get; }
        public bool TwoHanded { get; }
        public int Stock { get; }
    }
    
    [Serializable]
    public struct SavedStockItem
    {
        [SerializeField] private string itemId;
        [SerializeField] private int units;

        public SavedStockItem(string itemId, int units)
        {
            this.itemId = itemId;
            this.units = units;
        }

        public string ItemId => itemId;
        public int Units => units;
    }
    
    // One potion tier: the party's share cut and the chance bonus at approval, and the guild's brew cost per potion.
    [Serializable]
    public struct PotionTier
    {
        [SerializeField] private string name;
        [Tooltip("Percentage points taken off the party's reward share (70% → 60% for 10).")]
        [SerializeField, Range(0, 100)] private int shareCutPercent;
        [Tooltip("Added to the quest's final success chance (still capped at the maximum).")]
        [SerializeField, Range(0f, 1f)] private float chanceBonus;
        [Tooltip("Guild gold to brew one potion of this tier (paid at the morning brew).")]
        [SerializeField, Min(0)] private int brewCost;

        public PotionTier(string name, int shareCutPercent, float chanceBonus, int brewCost)
        {
            this.name = name;
            this.shareCutPercent = shareCutPercent;
            this.chanceBonus = chanceBonus;
            this.brewCost = brewCost;
        }

        public string Name => name;
        public int ShareCutPercent => shareCutPercent;
        public float ChanceBonus => chanceBonus;
        public int BrewCost => brewCost;
    }
    
    // Which potion: a tier (1-based; 0 = none) for one quest category.
    public readonly struct PotionKey : IEquatable<PotionKey>
    {
        public PotionKey(int tier, QuestCategory category)
        {
            Tier = tier;
            Category = category;
        }

        public int Tier { get; }
        public QuestCategory Category { get; }
        public bool IsNone => Tier <= 0;

        public static PotionKey None => default;

        public bool Equals(PotionKey other) => Tier == other.Tier && Category == other.Category;
        public override bool Equals(object obj) => obj is PotionKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Tier, (int)Category);
    }

    // Potions of one kind in stock, as stored in game.json.
    [Serializable]
    public struct SavedPotionStock
    {
        [SerializeField] private int tier;
        [SerializeField] private QuestCategory category;
        [SerializeField] private int count;

        public SavedPotionStock(int tier, QuestCategory category, int count)
        {
            this.tier = tier;
            this.category = category;
            this.count = count;
        }

        public int Tier => tier;
        public QuestCategory Category => category;
        public int Count => count;
    }


    // What the Applications screen shows for the current potion choice.
    public readonly struct PotionPreview
    {
        public PotionPreview(bool visible, string label, string effect, string warning, bool canAfford, PotionKey potion = default)
        {
            Visible = visible;
            Label = label;
            Effect = effect;
            Warning = warning;
            CanAfford = canAfford;
            Potion = potion;
        }

        public bool Visible { get; }
        public string Label { get; }
        public string Effect { get; }
        public string Warning { get; }
        public bool CanAfford { get; }

        // The chosen potion (None = no icon).
        public PotionKey Potion { get; }

        public static PotionPreview Hidden => new(false, string.Empty, string.Empty, string.Empty, true);
    }
    
    // The icon for one quest category's potions (GameConfig › Alchemy).
    [Serializable]
    public struct PotionIconEntry
    {
        [SerializeField] private QuestCategory category;
        [SerializeField] private Sprite icon;

        public QuestCategory Category => category;
        public Sprite Icon => icon;
    }
    #endregion
    
    #region Avatars
    // One catalog entry: which sheet a species + class combination uses. Empty species or class = any.
    [Serializable]
    public struct AvatarSheetEntry
    {
        [Tooltip("Empty = any species.")]
        [SerializeField] private SpeciesData species;
        [Tooltip("Empty = any class.")]
        [SerializeField] private ClassData classData;
        [SerializeField] private AdventurerSpriteSheet sheet;

        public SpeciesData Species => species;
        public ClassData Class => classData;
        public AdventurerSpriteSheet Sheet => sheet;

        // −1 if it doesn't apply to this combination; otherwise 2 = exact, 1 = one side matched, 0 = matches anyone.
        public int Specificity(SpeciesData forSpecies, ClassData forClass)
        {
            if (sheet == null)
                return -1;
            if (species != null && species != forSpecies)
                return -1;
            if (classData != null && classData != forClass)
                return -1;
            return (species ? 1 : 0) + (classData ? 1 : 0);
        }
    }
    
    // One frame of the walk bob: lift up and sway sideways (sheet pixels), lean (degrees, + = toward the sway side).
    // NOTE: default = standing still.
    public readonly struct AvatarBobPose
    {
        public int LiftPixels { get; }
        public int SwayPixels { get; }
        public float LeanDegrees { get; }

        public AvatarBobPose(int liftPixels, int swayPixels, float leanDegrees)
        {
            LiftPixels = liftPixels;
            SwayPixels = swayPixels;
            LeanDegrees = leanDegrees;
        }
    }
    #endregion
    
    #region Risks
    // What a quest did to one member: died, or an injury with its recovery days (None = unharmed).
    public readonly struct RiskResult
    {
        public RiskResult(Adventurer adventurer, bool died, InjurySeverity injury, int recoveryDays)
        {
            Adventurer = adventurer;
            Died = died;
            Injury = injury;
            RecoveryDays = recoveryDays;
        }

        public Adventurer Adventurer { get; }
        public bool Died { get; }
        public InjurySeverity Injury { get; }
        public int RecoveryDays { get; }
    }
    #endregion
    
    #region Parties
    // A party as stored in game.json.
    [Serializable]
    public struct SavedParty
    {
        [SerializeField] private int id;
        [SerializeField] private string name;
        [SerializeField] private int[] memberIds;
        [SerializeField] private int sharedSuccesses;
        [SerializeField] private int failStreak;
        
        public SavedParty(int id, string name, int[] memberIds, int sharedSuccesses, int failStreak)
        {
            this.id = id;
            this.name = name;
            this.memberIds = memberIds;
            this.sharedSuccesses = sharedSuccesses;
            this.failStreak = failStreak;
        }

        public int Id => id;
        public string Name => name;
        public int[] MemberIds => memberIds ?? Array.Empty<int>();
        public int SharedSuccesses => sharedSuccesses;
        public int FailStreak => failStreak;
    }
    
    // A group's successes together while not in a "party" (forms one at the threshold).
    [Serializable]
    public struct SavedLineup
    {
        [SerializeField] private string key;
        [SerializeField] private int successes;

        public SavedLineup(string key, int successes)
        {
            this.key = key;
            this.successes = successes;
        }

        // Sorted member ids, comma-separated ("3,7,12").
        public string Key => key;
        public int Successes => successes;
    }
    #endregion
    
    #region Lodging
    // Where an adventurer sleeps, as stored in game.json (room id; 0 = outside the guild).
    [Serializable]
    public struct SavedLodging
    {
        [SerializeField] private int adventurerId;
        [SerializeField] private int roomId;

        public SavedLodging(int adventurerId, int roomId)
        {
            this.adventurerId = adventurerId;
            this.roomId = roomId;
        }

        public int AdventurerId => adventurerId;
        public int RoomId => roomId;
    }
    #endregion
    
    #region Advancement
    // Whether an adventurer can start a class trial now: the classes they can choose, the fee, or why not.
    public readonly struct AdvancementCheck
    {
        private AdvancementCheck(bool canAdvance, bool atCap, IReadOnlyList<ClassData> options, long fee, string reason)
        {
            CanAdvance = canAdvance;
            IsAtClassCap = atCap;
            Options = options;
            Fee = fee;
            Reason = reason;
        }

        public bool CanAdvance { get; }

        // True when they've reached their class's level and rank caps (advancement is the way forward).
        public bool IsAtClassCap { get; }

        // The upgrades the guild's rank allows (empty when blocked).
        public IReadOnlyList<ClassData> Options { get; }
        public long Fee { get; }
        public string Reason { get; }

        public static AdvancementCheck NotAtCap() => new(false, false, Array.Empty<ClassData>(), 0, string.Empty);
        public static AdvancementCheck Blocked(long fee, string reason) => new(false, true, Array.Empty<ClassData>(), fee, reason);
        public static AdvancementCheck Eligible(IReadOnlyList<ClassData> options, long fee) => new(true, true, options, fee, string.Empty);
    }
    #endregion
    
    #region Speech
    // One topic's lines in the speech library.
    [Serializable]
    public struct SpeechLineSet
    {
        [SerializeField] private SpeechTopic topic;
        [SerializeField] private string[] lines;

        public SpeechLineSet(SpeechTopic topic, params string[] lines)
        {
            this.topic = topic;
            this.lines = lines;
        }

        public SpeechTopic Topic => topic;
        public IReadOnlyList<string> Lines => lines ?? Array.Empty<string>();
    }

    // A line an adventurer wants to say (times in in-game minutes).
    // NOTE: WaitsForArrival = kept while the speaker is away (quest results, level-ups, injuries); otherwise the request is
    // dropped as soon as their avatar is gone.
    public readonly struct SpeechRequest
    {
        public int AdventurerId { get; }
        public SpeechTopic Topic { get; }
        public int Priority { get; }
        public long ShowAtMinute { get; }
        public long ExpiresAtMinute { get; }
        public bool WaitsForArrival { get; }

        public SpeechRequest(int adventurerId, SpeechTopic topic, int priority, long showAtMinute, long expiresAtMinute,
            bool waitsForArrival)
        {
            AdventurerId = adventurerId;
            Topic = topic;
            Priority = priority;
            ShowAtMinute = showAtMinute;
            ExpiresAtMinute = expiresAtMinute;
            WaitsForArrival = waitsForArrival;
        }
    }
    #endregion
}