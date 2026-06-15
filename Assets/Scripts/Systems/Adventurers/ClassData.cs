// ScriptableObject defining all data for one adventurer class.
// AdventurerConfig holds the full pool; the factory picks classes from it.
// Structs CategoryAffinitySet and RankUpDurationSet are values types (not classes) and share this file to keep class-specific data self-contained.

using UnityEngine;

[CreateAssetMenu(fileName = "ClassData_New", menuName = "Guild Manager/Class Data")]
public class ClassData : ScriptableObject
{
    #region Identity
    [Header("Identity")]
    [Tooltip("The enum value this asset represents. Must be unique across all ClassData assets " +
             "in the pool — the factory uses this to find the right asset.")]
    [SerializeField] private AdventurerClass adventurerClass;
    
    [Tooltip("Name displayed in UI panels and logs (e.g. 'Fighter', 'Archer').")]
    [SerializeField] private string displayName = "Class";
    #endregion
    
    #region Base Stats
    [Header("Base Stats at level 1")]
    [SerializeField, Min(1f)] private float baseHp = 100f;
    [SerializeField, Min(0f)] private float baseDamage = 10f;
    [SerializeField, Min(0f)] private float baseSpeed = 8f;
    
    [Header("Per-Level Stat Growth")]
    [SerializeField, Min(0f)] private float hpPerLevel = 8f;
    [SerializeField, Min(0f)] private float damagePerLevel = 2f;
    [SerializeField, Min(0f)] private float speedPerLevel = 0.3f;
    #endregion
    
    #region Quest Category Affinities
    [Header("Quest Category Affinities")]
    [Tooltip("Preferred gives a positive flat modifier to success chance per member. " +
             "Disliked gives a negative modifier. Neutral gives none.")]
    [SerializeField] private CategoryAffinitySet categoryAffinities;
    #endregion
    
    #region Rank-Up Quest
    [Header("Rank-Up Quest")]
    [Tooltip("Quest category used for this class's rank-up quests. " +
             "The rank-up success chance formula uses this class's affinity for the category.")]
    [SerializeField] private QuestCategory rankUpQuestCategory = QuestCategory.Combat;

    [Tooltip("Duration in in-game hours for rank-up quests, keyed by the rank being advanced FROM " +
             "(e.g. fToE is how long a Fighter's F→E rank-up quest takes). " +
             "Two different classes at the same rank can have different durations.")]
    [SerializeField] private RankUpDurationSet rankUpDurations;
    #endregion
    
    #region Public Accessors
    public AdventurerClass AdventurerClass => adventurerClass;
    public string DisplayName => displayName;
    public QuestCategory RankUpCategory => rankUpQuestCategory;
    #endregion

    #region Helpers
    // Stat values for a given level. Level is clamped to 1 so negatives are safe.
    public float GetHp(int level) 
        => baseHp + hpPerLevel * (Mathf.Max(1, level) - 1);
    public float GetDamage(int level) 
        => baseDamage + damagePerLevel * (Mathf.Max(1, level) - 1);
    public float GetSpeed(int level) 
        => baseSpeed + speedPerLevel * (Mathf.Max(1, level) - 1);

    // Returns this class's affinity for a given quest category
    public CategoryAffinity GetAffinity(QuestCategory category) 
        => categoryAffinities.Get(category);
    
    // Returns the rank-up quest duration for an adventurer advancing FROM currentRank.
    public float GetRankUpDuration(QuestRank currentRank) 
        => rankUpDurations.Get(currentRank);
    #endregion
    
#if UNITY_EDITOR
    private void Reset()
    {
        // Default all affinities to Neutral on asset creation.
        // Without this, structs default to 0 = Preferred since it is the first enum value.
        categoryAffinities = new CategoryAffinitySet
        {
            combat = CategoryAffinity.Neutral,
            gathering = CategoryAffinity.Neutral,
            escort = CategoryAffinity.Neutral,
            delivery = CategoryAffinity.Neutral,
            subjugation = CategoryAffinity.Neutral,
            exploration = CategoryAffinity.Neutral,
            investigation = CategoryAffinity.Neutral,
            dungeon = CategoryAffinity.Neutral,
        };

        rankUpDurations = new RankUpDurationSet
        {
            fToE = 24f,
            eToD = 36f,
            dToC = 48f,
            cToB = 60f,
            bToA = 72f,
            aToS = 96f,
            sToSpecial = 120f,
        };
    }
#endif
}
#region Structs
// Struct — not a class. One named field per QuestCategory for clear inspector display.
// The Get() method is the only lookup point; adding new categories requires one
// new field here and one new case in the switch.
[System.Serializable]
public struct CategoryAffinitySet
{
    [Tooltip("Affinity for Combat quests.")]
    public CategoryAffinity combat;

    [Tooltip("Affinity for Gathering quests.")]
    public CategoryAffinity gathering;

    [Tooltip("Affinity for Escort quests.")]
    public CategoryAffinity escort;

    [Tooltip("Affinity for Delivery quests.")]
    public CategoryAffinity delivery;

    [Tooltip("Affinity for Subjugation quests.")]
    public CategoryAffinity subjugation;

    [Tooltip("Affinity for Exploration quests.")]
    public CategoryAffinity exploration;

    [Tooltip("Affinity for Investigation quests.")]
    public CategoryAffinity investigation;

    [Tooltip("Affinity for Dungeon Delving quests.")]
    public CategoryAffinity dungeon;
    
    public CategoryAffinity Get(QuestCategory category)
    {
        return category switch
        {
            QuestCategory.Combat => combat,
            QuestCategory.Gathering => gathering,
            QuestCategory.Escort => escort,
            QuestCategory.Delivery => delivery,
            QuestCategory.Subjugation => subjugation,
            QuestCategory.Exploration => exploration,
            QuestCategory.Investigation => investigation,
            QuestCategory.Dungeon => dungeon,
            _ => CategoryAffinity.Neutral
        };
    }
}

// Struct — not a class. One named field per advancing rank for clear inspector display.
// Each class can set different hours per rank. fToE = duration when advancing FROM F.
[System.Serializable]
public struct RankUpDurationSet
{
    [Tooltip("F → E rank-up quest duration in in-game hours.")]
    [Min(1f)] public float fToE;

    [Tooltip("E → D rank-up quest duration in in-game hours.")]
    [Min(1f)] public float eToD;

    [Tooltip("D → C rank-up quest duration in in-game hours.")]
    [Min(1f)] public float dToC;

    [Tooltip("C → B rank-up quest duration in in-game hours.")]
    [Min(1f)] public float cToB;

    [Tooltip("B → A rank-up quest duration in in-game hours.")]
    [Min(1f)] public float bToA;

    [Tooltip("A → S rank-up quest duration in in-game hours.")]
    [Min(1f)] public float aToS;

    [Tooltip("S → Special rank-up quest duration in in-game hours.")]
    [Min(1f)] public float sToSpecial;

    public float Get(QuestRank currentRank)
    {
        return currentRank switch
        {
            QuestRank.F => fToE,
            QuestRank.E => eToD,
            QuestRank.D => dToC,
            QuestRank.C => cToB,
            QuestRank.B => bToA,
            QuestRank.A => aToS,
            QuestRank.S => sToSpecial,
            _ => 24f
        };
    }
}
#endregion