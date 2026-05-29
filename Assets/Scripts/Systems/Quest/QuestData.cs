// Runtime instance of a quest, created at the reception desk from a QuestRequestData template.
// Stores its own copies of all relevant fields so it is self-contained and independent of the source ScriptableObject after creation.
// All state transitions are gated methods; only QuestManager should call them.

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestData
{
    #region Identity
    // GUID generated at creation
    private string _questId;
    // RequestId of the QuestRequestData thi came from
    private string _sourceRequestId;
    private string _questName;
    private string _description;
    private string _location;
    #endregion
    
    #region Classification
    private QuestCategory _category;
    // Player-chosen rank (base ± 1)
    private QuestRank _rank;
    #endregion
    
    #region Parameters
    // Gold offered to the adventuring party. Player-chosen 0 ... maxReward.
    private int _partyReward;
    // Remainder that goes to guild funds on success. = maxReward - partyReward.
    private int _guildReward;
    // Max party size for this quest (1-5)
    private int _partyLimit;
    // Shared deadline: expiry if not dispatched, deadline if dispatched
    private int _timeLimitHours;
    private string[] _hiddenTags;
    #endregion
    
    #region Status
    private QuestStatus _status;
    #endregion
    
    #region Timing
    // All timestamps are total game-hours elapsed since session start, as tracked by QuestManager.
    // -1 means the relevant event has not occurred yet.
    // Set by Post()
    private float _postedHour;
    // postedHour + timeLimitHours; set by Post()
    private float _expiryHour;
    // Set by Dispatch()
    private float _dispatchedHour;
    // Calculated completion time; set by Dispatch()
    private float _resolveAtHour;
    #endregion
    
    #region Applications
    private List<QuestApplication> _applications;
    // Null until Dispatch() is called 
    private QuestApplication _approvedApplication;
    #endregion
    
    #region Constructor
    public QuestData(QuestRequestData source, QuestRank chosenRank, int partyReward)
    {
        if (!source)
        {
            Debug.LogError("[QuestData] Created with a null QuestRequestData source.");
            return;
        }

        _questId = Guid.NewGuid().ToString();
        _sourceRequestId = source.RequestId;
        _questName = source.RequestName;
        _description = source.Description;
        _location = source.Location;
        _category = source.Category;
        _rank = chosenRank;
        
        // Clamp to valid range even if caller passes an out-of-bounds value.
        _partyReward = Mathf.Clamp(partyReward, 0, source.MaxReward);
        _guildReward = source.MaxReward - _partyReward;

        _partyLimit = source.PartyLimit;
        _timeLimitHours =  source.TimeLimitHours;
        _hiddenTags = source.GetHiddenTagsCopy();

        _status = QuestStatus.Unposted;
        _postedHour = -1f;
        _expiryHour = -1f;
        _dispatchedHour = -1f;
        _resolveAtHour = -1f;
        
        _applications = new List<QuestApplication>();
        _approvedApplication = null;
    }
    #endregion
    
    #region Public Accessors
    public string QuestId => _questId;
    public string SourceRequestId => _sourceRequestId;
    public string QuestName => _questName;
    public string Description => _description;
    public string Location => _location;
    public QuestCategory Category => _category;
    public QuestRank Rank => _rank;
    public int PartyReward => _partyReward;
    public int GuildReward => _guildReward;
    public int PartyLimit => _partyLimit;
    public int TimeLimitHours => _timeLimitHours;
    public QuestStatus Status => _status;
    public float PostedHour => _postedHour;
    public float ExpiryHour => _expiryHour;
    public float DispatchedHour => _dispatchedHour;
    public float ResolveAtHour => _resolveAtHour;
    
    public QuestApplication ApprovedApplication => _approvedApplication;
    public IReadOnlyList<QuestApplication> Applications => _applications;
    #endregion
    
    #region Gold Distribution
    // Base gold each party member receives (integer floor of an even split).
    public int GetGoldPerMember(int partySize)
    {
        if (partySize <= 0)
            return 0;
        return _partyReward / partySize;
    }
    // Remainder gold given to the party leader on top of their base share.
    // Example: 100 gold / 3 member -> 33 each; leader gets +1 (total 34).
    public int GetLeaderBonus(int partySize)
    {
        if (partySize <= 0)
            return 0;
        return _partyReward - (GetGoldPerMember(partySize) * partySize);
    }
    #endregion
    
    #region Status Transitions
    // Moves from Unposted → Posted and starts the shared expiry/deadline clock.
    public bool Post(float currentGameHour)
    {
        if (_status != QuestStatus.Unposted)
        {
            Debug.LogWarning($"[QuestData] '{_questName}' cannot be posted — status is {_status}.");
            return false;
        }
        _status = QuestStatus.Posted;
        _postedHour = currentGameHour;
        _expiryHour = currentGameHour + _timeLimitHours;
        return true;
    }
    
    // Moves from Posted → InProgress.
    // resolveAtHour is computed externally by QuestManager using the formula.
    // Also approves the given application and rejects all remaining pending ones.
    public bool Dispatch(float currentGameHour, float resolveAtHour, QuestApplication approved)
    {
        if (_status != QuestStatus.Posted)
        {
            Debug.LogWarning($"[QuestData] '{_questName}' cannot be dispatched — status is {_status}.");
            return false;
        }
        if (approved == null)
        {
            Debug.LogError($"[QuestData] '{_questName}' dispatch failed — approved application is null.");
            return false;
        }
        _status = QuestStatus.InProgress;
        _dispatchedHour = currentGameHour;
        _resolveAtHour = resolveAtHour;
        _approvedApplication = approved;
        approved.Approve();
        RejectRemainingApplications();
        return true;
    }
    
    // Moves from InProgress → Completed.
    public bool Complete()
    {
        if (_status != QuestStatus.InProgress)
        {
            Debug.LogWarning($"[QuestData] '{_questName}' cannot complete — status is {_status}.");
            return false;
        }
        _status = QuestStatus.Completed;
        return true;
    }
    
    // Moves from InProgress → Failed (party wiped, or deadline reached while in progress).
    public bool Fail()
    {
        if (_status != QuestStatus.InProgress)
        {
            Debug.LogWarning($"[QuestData] '{_questName}' cannot fail — status is {_status}.");
            return false;
        }
        _status = QuestStatus.Failed;
        return true;
    }

    // Moves from Posted → Expired (deadline passed with no approved application).
    public bool Expire()
    {
        if (_status != QuestStatus.Posted)
        {
            Debug.LogWarning($"[QuestData] '{_questName}' cannot expire — status is {_status}.");
            return false;
        }
        _status = QuestStatus.Expired;
        return true;
    }
    #endregion
    
    #region Application Management
    // Registers a new application.
    // Only accepted while the quest is Posted.
    public bool AddApplication(QuestApplication application)
    {
        if (_status != QuestStatus.Posted)
        {
            Debug.LogWarning($"[QuestData] '{_questName}' is not accepting applications " +
                             $"(status: {_status}).");
            return false;
        }
        if (application == null)
        {
            Debug.LogError($"[QuestData] Null application passed to '{_questName}'.");
            return false;
        }
        _applications.Add(application);
        return true;
    }
    
    // Rejects all still-pending applications.
    // Called automatically inside Dispatch().
    public void RejectRemainingApplications()
    {
        foreach (var app in _applications.Where(app => app.Status == ApplicationStatus.Pending))
            app.Reject();
    }
    #endregion
    
    #region Timing Queries
    // Hours remaining before the quest expires or the dispatched party's deadline hits.
    // Returns 0 if the quest is not in a timed state.
    public float GetRemainingHours(float currentGameHour)
    {
        if (_status != QuestStatus.Posted && _status != QuestStatus.InProgress)
            return 0f;
        return Mathf.Max(0f, _expiryHour - currentGameHour);
    }
    
    // True if the shared deadline has passed.
    // Guards against false positives before Pos() has been called by requiring a valid expiryHour.
    public bool IsDeadlinePassed(float currentGameHour)
        => _expiryHour > 0f && currentGameHour >= _expiryHour;
    #endregion
    
    #region Utility
    public string[] GetHiddenTagsCopy()
        => (string[])_hiddenTags.Clone();
    #endregion
}