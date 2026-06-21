// ScriptableObject defining all data for one adventurer class: identity, rarity, unlock
// requirements, base stats + per-level growth, and quest category affinities.
// ClassDatabase holds the full pool; ClassRegistry tracks which of these are currently unlocked.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClassData_New", menuName = "Guild Manager/Adventurer/Class Data")]
public class ClassData : ScriptableObject
{
    #region Identity
    [Header("Identity")]
    [Tooltip("The enum value this asset represents. Must be unique across the ClassDatabase pool.")]
    [SerializeField] private AdventurerClass adventurerClass;

    [Tooltip("Name displayed in UI panels and logs (e.g. 'Fighter', 'Archer').")]
    [SerializeField] private string displayName = "Class";

    [Tooltip("How rare this class is. Purely descriptive for now — does not affect roll odds yet.")]
    [SerializeField] private ClassRarity rarity = ClassRarity.Common;
    #endregion

    #region Unlock Requirements
    [Header("Unlock Requirements")]
    [Tooltip("How this class becomes available: automatically at a guild rank, or manually via " +
             "training in a room (hook reserved for the Build system refactor).")]
    [SerializeField] private ClassUnlockMethod unlockMethod = ClassUnlockMethod.Start;

    [Tooltip("Guild rank required to unlock this class. Only used when UnlockMethod is RankUp.")]
    [SerializeField] private QuestRank minimumRank = QuestRank.F;

    [Tooltip("An adventurer must be at least this level for this class to be eligible when rolling " +
             "a random adventurer. Does NOT gate whether the class itself is unlocked guild-wide.")]
    [SerializeField, Min(1)] private int minimumLevel = 1;
    #endregion

    #region Base Stats
    [Header("Base Stats at Level 1")]
    [SerializeField, Min(1f)] private float baseHp = 100f;
    [SerializeField, Min(0f)] private float baseStrength = 10f;
    [SerializeField, Min(0f)] private float baseDexterity = 10f;

    [Header("Per-Level Stat Growth")]
    [SerializeField, Min(0f)] private float hpPerLevel = 8f;
    [SerializeField, Min(0f)] private float strengthPerLevel = 1.5f;
    [SerializeField, Min(0f)] private float dexterityPerLevel = 1.5f;
    #endregion

    #region Quest Category Affinities
    [Header("Quest Category Affinities")]
    [Tooltip("Categories this class gets a success-chance bonus for.")]
    [SerializeField] private List<QuestCategory> preferredCategories = new();

    [Tooltip("Categories this class gets a success-chance penalty for.")]
    [SerializeField] private List<QuestCategory> dislikedCategories = new();

    [Tooltip("Quest category used for this class's future rank-up quests.")]
    [SerializeField] private QuestCategory rankUpQuestCategory = QuestCategory.Combat;
    #endregion

    // TODO: Traits system — reserved for a future pass, once TraitData assets exist.
    // [SerializeField] private TraitData[] availableTraits;

    #region Public Accessors
    public AdventurerClass AdventurerClass => adventurerClass;
    public string DisplayName => displayName;
    public ClassRarity Rarity => rarity;
    public ClassUnlockMethod UnlockMethod => unlockMethod;
    public QuestRank MinimumRank => minimumRank;
    public int MinimumLevel => minimumLevel;
    public QuestCategory RankUpQuestCategory => rankUpQuestCategory;
    public IReadOnlyList<QuestCategory> PreferredCategories => preferredCategories;
    public IReadOnlyList<QuestCategory> DislikedCategories => dislikedCategories;
    #endregion

    #region Helpers
    // Computes this class's full stat block at the given level. Level is clamped to 1.
    public AdventurerStats GetStats(int level)
    {
        var lvl = Mathf.Max(1, level);
        var maxHp = baseHp + hpPerLevel * (lvl - 1);
        return new AdventurerStats
        {
            maxHp = maxHp,
            currentHp = maxHp,
            strength = baseStrength + strengthPerLevel * (lvl - 1),
            dexterity = baseDexterity + dexterityPerLevel * (lvl - 1)
        };
    }

    // Returns this class's affinity for a given quest category.
    public CategoryAffinity GetAffinity(QuestCategory category)
    {
        if (preferredCategories.Contains(category))
            return CategoryAffinity.Preferred;
        if (dislikedCategories.Contains(category))
            return CategoryAffinity.Disliked;
        return CategoryAffinity.Neutral;
    }
    #endregion
}