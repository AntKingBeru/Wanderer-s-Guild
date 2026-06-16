// Runtime data for a single adventurer.
// Stores all states, exposes clean transition methods, and keeps validation local, so AdventurerManager only handles timing and decisions - not illegal-state prevention.
// ClassData is NOT stored as a field to remain serialization friendly.
// Pass ClassData through AdventurerManager whenever stat recalculation is needed.

using UnityEngine;

[System.Serializable]
public class AdventurerData
{
    #region Identity
    private string _id;
    private string _name;
    private AdventurerClass _class;
    private QuestRank _rank;
    private int _level;
    #endregion
    
    #region Stats (pre-computed from class + level; recalculated on level-up)
    private float _maxHp;
    private float _currentHp;
    private float _damage;
    private float _speed;
    #endregion
    
    #region Progress
    private int _rankPoints;
    private int _experiencePoints;
    private int _xpToNextLevel;
    #endregion
    
    #region Party
    private string _partyId;
    private bool _isPartyLeader;
    #endregion
    
    #region Gold
    private int _gold;
    #endregion
    
    #region Lodging
    private LodgingState _lodgingState;
    private string _lodgingRoomId;
    #endregion
    
    #region Maintenance
    private int _missedSleepNights;
    private int _daysWithoutFood;
    #endregion
    
    #region Quest State
    private AdventurerStatus _status;
    private string _currentQuestId;
    private string _currentApplicationId;
    #endregion
    
    #region Rank-Up State
    private bool _rankUpEligible;
    private string _rankApplicationId;
    private float _rankUpQuestStartHour = -1f;
    private float _rankUpQuestEndHour = -1f;
    private int _rankUpSuccessesRequired;
    private float _rankUpCooldownEndHour = -1f;
    #endregion
    
    #region Constructor
    public AdventurerData(string id, string name, ClassData classData, QuestRank rank, int level, AdventurerConfig config)
    {
        _id = id;
        _name = name;
        _class = classData.AdventurerClass;
        _rank = rank;
        _level = level;

        _maxHp = classData.GetHp(level);
        _currentHp = _maxHp;
        _damage = classData.GetDamage(level);
        _speed = classData.GetSpeed(level);

        _xpToNextLevel = config.GetXpForLevel(level);
        _status = AdventurerStatus.Idle;
        _lodgingState = LodgingState.Nowhere;
    }
    #endregion
    
    #region Public Accessors
    public string Id => _id;
    public string Name => _name;
    public AdventurerClass Class => _class;
    public QuestRank Rank => _rank;
    public int Level => _level;
    public float MaxHp => _maxHp;
    public float CurrentHp => _currentHp;
    public float Damage => _damage;
    public float Speed => _speed;
    public int RankPoints => _rankPoints;
    public int ExperiencePoints => _experiencePoints;
    public int XpToNextLevel => _xpToNextLevel;
    public string PartyId => _partyId;
    public bool IsPartyLeader => _isPartyLeader;
    public int Gold => _gold;
    public LodgingState LodgingState => _lodgingState;
    public string LodgingRoomId => _lodgingRoomId;
    public int MissedSleepNights => _missedSleepNights;
    public int DaysWithoutFood => _daysWithoutFood;
    public AdventurerStatus Status => _status;
    public string CurrentQuestId => _currentQuestId;
    public string CurrentApplicationId => _currentApplicationId;
    public bool RankUpEligible => _rankUpEligible;
    public string RankApplicationId => _rankApplicationId;
    public float RankUpQuestStartHour => _rankUpQuestStartHour;
    public float RankUpQuestEndHour => _rankUpQuestEndHour;
    public int RankUpSuccessesRequired => _rankUpSuccessesRequired;
    public float RankUpCooldownEndHour => _rankUpCooldownEndHour;
    
    public bool IsSolo => string.IsNullOrEmpty(PartyId);
    public bool OnRankUpQuest => _status == AdventurerStatus.OnRankUpQuest;
    #endregion
    
    #region Power & Penalty Queries
    public float CalculatePower(AdventurerConfig config)
    {
        var raw = _maxHp * config.HpWeight
                  + _damage * config.DamageWeight
                  + _speed * config.SpeedWeight;
        return raw * config.GetRankMultiplier(_rank);
    }
    
    public float GetMaintenancePenalty(AdventurerConfig config)
        => config.GetSleepMissedPenalty(_missedSleepNights) + config.GetFoodDeprivedPenalty(_daysWithoutFood);
    
    public bool IsRankUpQuestComplete(float currentGameHour)
        => _rankUpQuestEndHour > 0f && currentGameHour >= _rankUpQuestEndHour;

    public bool CanReapplyForRankUp(float currentGameHour)
        => _rankUpEligible
           && _rankUpSuccessesRequired <= 0
           && (currentGameHour >= _rankUpCooldownEndHour || _rankUpCooldownEndHour < 0f);
    #endregion
    
    #region Regular Quest Transitions
    public bool ApplyToQuest(string applicationId)
    {
        if (_status != AdventurerStatus.Idle)
        {
            Debug.LogWarning($"[AdventurerData] '{_name}' cannot apply - status is {_status}.");
            return false;
        }
        _status = AdventurerStatus.AppliedToQuest;
        _currentApplicationId = applicationId;
        return true;
    }

    public bool DispatchToQuest(string questId)
    {
        if (_status != AdventurerStatus.AppliedToQuest)
        {
            Debug.LogWarning($"[AdventurerData] '{_name}' cannot be dispatched — status is {_status}.");
            return false;
        }
        _status = AdventurerStatus.OnQuest;
        _currentQuestId = questId;
        _currentApplicationId = null;
        return true;
    }

    public bool ReturnFromQuest()
    {
        if (_status != AdventurerStatus.OnQuest)
        {
            Debug.LogWarning($"[AdventurerData] '{_name}' is not OnQuest - status is {_status}.");
            return false;
        }
        _status = AdventurerStatus.Idle;
        _currentApplicationId = null;
        return true;
    }

    public bool CancelQuestApplication()
    {
        if (_status != AdventurerStatus.AppliedToQuest)
        {
            Debug.LogWarning($"[AdventurerData] '{_name}' has no active application to cancel.");
            return false;
        }
        _status = AdventurerStatus.Idle;
        _currentApplicationId = null;
        return true;
    }
    #endregion
    
    #region Rank-Up Transitions
    public void SetRankUpEligible(bool eligible)
        => _rankUpEligible = eligible;
    
    public void SetRankUpApplication(string applicationId)
        => _rankApplicationId = applicationId;
    
    public void ClearRankUpApplication() 
        => _rankApplicationId = null;

    public bool DispatchToRankUpQuest(float startHour, float endHour)
    {
        if (_status != AdventurerStatus.Idle && _status != AdventurerStatus.AppliedToQuest)
        {
            Debug.LogWarning($"[AdventurerData] '{_name}' cannot start rank-up quest - currently {_status}.");
            return false;
        }

        _status = AdventurerStatus.OnRankUpQuest;
        _rankUpQuestStartHour = startHour;
        _rankUpQuestEndHour = endHour;
        return true;
    }

    public void CompleteRankUpQuest()
    {
        _rank = (QuestRank)((int)_rank + 1);
        _rankPoints = 0;
        _rankUpEligible = false;
        _rankUpQuestStartHour = -1f;
        _rankUpQuestEndHour = -1f;
        _rankUpSuccessesRequired = 0;
        _rankUpCooldownEndHour = -1f;
        _status = AdventurerStatus.Idle;
    }

    public void FailRankUpQuest(float cooldownEndHour, AdventurerConfig config)
    {
        _rankUpEligible = false;
        _rankUpQuestStartHour = -1f;
        _rankUpQuestEndHour = -1f;
        _rankUpCooldownEndHour = cooldownEndHour;
        _rankUpSuccessesRequired = config.RankUpRetrySuccessesRequired;
        _status = AdventurerStatus.Idle;
    }
    #endregion
    
    #region Progress
    // Adds rank points and checks whether this adventurer is now eligible for a rank-up quest.
    // Returns true the first time eligibility is gained (so the manager can create the application).
    // Rank-up eligibility cap rules (via ProgressionSystem):
    //   - A-rank and above → can only rank up TO the guild's current rank.
    //   - Below A-rank → can rank up to guild rank + 1.
    public bool AddRankPoints(int amount, AdventurerConfig config)
    {
        _rankPoints += amount;
        if (_rankUpEligible)
            return false;

        // Determine the highest rank this adventurer is currently allowed to reach.
        QuestRank maxTarget;
        if (ProgressionSystem.Instance)
        {
            maxTarget = ProgressionSystem.Instance.GetMaxAdventurerRankUpTarget(_rank);
        }
        else
        {
            // Fallback: use the config cap (editor / testing scenes without ProgressionSystem).
            var nextIndex = (int)_rank + 1;
            maxTarget = (QuestRank)Mathf.Min(nextIndex, (int)config.GuildRankCap);
        }

        // Cannot rank up if already at or above the allowed ceiling.
        if ((int)_rank >= (int)maxTarget)
            return false;

        if (_rankPoints >= config.GetRankPointThreshold(_rank))
        {
            _rankUpEligible = true;
            return true;
        }
        return false;
    }

    public bool AddExperience(int amount, ClassData classData, AdventurerConfig config)
    {
        _experiencePoints += amount;
        if (_level >= config.MaxLevel)
            return false;
        var leveledUp = false;
        while (_experiencePoints >= _xpToNextLevel && _level < config.MaxLevel)
        {
            _experiencePoints -= _xpToNextLevel;
            LevelUp(classData, config);
            leveledUp = true;
        }
        return leveledUp;
    }

    public void OnRegularQuestSucceeded()
    {
        if (_rankUpSuccessesRequired > 0)
            _rankUpSuccessesRequired--;
    }
    #endregion
    
    #region Gold
    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[AdventurerData] AddGold called with negative amount. Use SpendGold instead.");
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
    
    #region Maintenance
    public void RecordSleepMissed() => _missedSleepNights++;
    public void ResetSleep() => _missedSleepNights = 0;
    
    public void RecordFoodDeprived() => _daysWithoutFood++;
    public void ResetFood() => _daysWithoutFood = 0;
    #endregion
    
    #region Party
    public void SetParty(string partyId, bool isLeader)
    {
        _partyId = partyId;
        _isPartyLeader = isLeader;
    }

    public void ClearParty()
    {
        _partyId = null;
        _isPartyLeader = false;
    }
    #endregion
    
    #region Lodging
    public void SetLodging(LodgingState state, string roomId = null)
    {
        _lodgingState = state;
        _lodgingRoomId = (state == LodgingState.InGuild) ? roomId : null;
    }
    #endregion
    
    #region Private Helpers
    private void LevelUp(ClassData classData, AdventurerConfig config)
    {
        _level++;
        _maxHp = classData.GetHp(_level);
        _currentHp = _maxHp;
        _damage = classData.GetDamage(_level);
        _speed = classData.GetSpeed(_level);
        _xpToNextLevel = config.GetXpForLevel(_level);
    }
    #endregion
}