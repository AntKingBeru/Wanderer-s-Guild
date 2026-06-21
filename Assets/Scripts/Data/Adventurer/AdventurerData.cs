// Runtime data for a single adventurer. Construction is restricted to AdventurerBuilder
// (Builder pattern) so every instance is always fully and validly initialized.
// Exposes validated transition methods for leveling, ranking, and gold so AdventurerManager
// only handles timing/orchestration — never illegal-state prevention.

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AdventurerData
{
    #region Identity
    private string _id;
    private string _name;
    private AdventurerClass _classType;
    private QuestRank _rank;
    private int _level;
    #endregion
    
    #region Stats
    private AdventurerStats _stats;
    #endregion
    
    #region Progress
    private int _rankPoints;
    private bool _rankUpEligible;
    private int _experiencePoints;
    private int _xpToNextLevel;
    #endregion
    
    #region Gold
    private int _gold;
    #endregion

    #region Status
    private AdventurerStatus _status;
    #endregion
    
    #region Reserved For Future Systems (NOT YET IMPLEMENTED)
    // TODO: Housing/Bedroom system — wire up once the Build system's room-assignment is refactored.
    // Always null until then.
    private string _bedroomRoomId;

    // TODO: Equipment system — wire up once item/equipment assets exist. Always null/empty until then.
    private string _equippedWeaponId;
    private string _equippedArmorId;
    private readonly List<string> _equippedArtifactIds = new(); // Max 3, once implemented.
    #endregion
    
    #region Constructor
    // Internal — only AdventurerBuilder may construct an AdventurerData.
    internal AdventurerData(string id, string name, ClassData classData, QuestRank rank, int level,
        int startingGold, AdventurerConfig config)
    {
        _id = id;
        _name = name;
        _classType = classData.AdventurerClass;
        _rank = rank;
        _level = level;
        _stats = classData.GetStats(level);
        _xpToNextLevel = config.GetXpForLevel(level);
        _gold = startingGold;
        _status = AdventurerStatus.Idle;
    }
    #endregion
    
    #region Public Accessors
    public string Id => _id;
    public string Name => _name;
    public AdventurerClass ClassType => _classType;
    public QuestRank Rank => _rank;
    public int Level => _level;
    public AdventurerStats Stats => _stats;
    public int RankPoints => _rankPoints;
    public bool RankUpEligible => _rankUpEligible;
    public int ExperiencePoints => _experiencePoints;
    public int XpToNextLevel => _xpToNextLevel;
    public int Gold => _gold;
    public AdventurerStatus Status => _status;
    public bool IsAlive => _status != AdventurerStatus.Dead;

    // TODO: Housing/Equipment accessors — reserved, always null/empty until those systems exist.
    public string BedroomRoomId => _bedroomRoomId;
    public string EquippedWeaponId => _equippedWeaponId;
    public string EquippedArmorId => _equippedArmorId;
    public IReadOnlyList<string> EquippedArtifactIds => _equippedArtifactIds;
    #endregion
    
    #region Experience & Leveling
    // Adds XP and applies as many level-ups as the amount covers. Returns true if at least one level-up occurred.
    public bool AddExperience(int amount, ClassData classData, AdventurerConfig config)
    {
        if (amount <= 0 || _level >= config.MaxLevel)
            return false;

        _experiencePoints += amount;
        var leveledUp = false;
        while (_experiencePoints >= _xpToNextLevel && _level < config.MaxLevel)
        {
            _experiencePoints -= _xpToNextLevel;
            LevelUp(classData, config);
            leveledUp = true;
        }
        return leveledUp;
    }

    private void LevelUp(ClassData classData, AdventurerConfig config)
    {
        _level++;
        _stats = classData.GetStats(_level);
        _xpToNextLevel = config.GetXpForLevel(_level);
    }
    #endregion
    
    #region Ranking
    // Adds rank points and flags eligibility once the threshold is crossed. Returns true the
    // first time eligibility is gained. Actual promotion happens via PromoteRank() — kept as a
    // separate step so the future Quest system can gate it behind a rank-up quest.
    public bool AddRankPoints(int amount, AdventurerConfig config)
    {
        if (amount <= 0 || _rankUpEligible)
            return false;

        _rankPoints += amount;

        var maxTarget = ProgressionSystem.Instance
            ? ProgressionSystem.Instance.GetMaxAdventurerRankUpTarget(_rank)
            : (QuestRank)Mathf.Min((int)_rank + 1, (int)QuestRank.Special);

        if (_rank >= maxTarget)
            return false;

        if (_rankPoints < config.GetRankPointThreshold(_rank))
            return false;

        _rankUpEligible = true;
        return true;
    }

    // Promotes the adventurer one rank and resets rank progress. Requires RankUpEligible.
    // This is the hook the Quest system will call once rank-up quests are rebuilt.
    public bool PromoteRank()
    {
        if (!_rankUpEligible)
        {
            Debug.LogWarning($"[AdventurerData] '{_name}' is not eligible to rank up.");
            return false;
        }

        _rank = (QuestRank)Mathf.Min((int)_rank + 1, (int)QuestRank.Special);
        _rankPoints = 0;
        _rankUpEligible = false;
        return true;
    }
    #endregion
    
    #region Gold
    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[AdventurerData] AddGold called with a negative amount. Use SpendGold instead.");
            return;
        }
        _gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (amount > _gold)
            return false;
        _gold -= amount;
        return true;
    }
    #endregion

    #region Status
    // Marks the adventurer as dead, removing them from active consideration.
    // AdventurerManager decides whether to keep or remove the roster entry.
    public void MarkAsDead() => _status = AdventurerStatus.Dead;
    #endregion
}