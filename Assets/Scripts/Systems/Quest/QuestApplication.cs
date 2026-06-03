// Represents a single party's application for a posted quest.
// Created by QuestManager when adventurers submit an application.
// Success chance is computed at submission time and stored here so the guild manager sees a stable value when reviewing - it does not change after submission even if time passes

using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class QuestApplication
{
    #region Identity
    // GUID, unique per application
    private string _applicationId;
    // ID of the quest this application targets
    private string _questId;
    private int _earlyFailureMarks;
    #endregion
    
    #region Party
    // Adventurer IDs referencing entries in AdventurerManager.
    // Stored as string so this class compiles and functions before the adventurer system exists.
    private string[] _partyMemberIds;
    
    // For temporary parties, the first adventurer to apply is the leader.
    // For permanent parties, this is the designated party leader.
    private string _leaderId;

    private bool _isTemporaryParty;
    #endregion
    
    #region Calculation Results
    // Combined party power passed in from QuestManager. Stored for display and for recalculation if needed.
    // Placeholder values will be used until the adventurer system defines real stats.
    private float _partyStrength;
    
    // Final success probability in [0, 1] after applying rank efficiency and category affinity modifiers.
    // Computed by QuestManager at submission time.
    private float _successChance;
    #endregion
    
    #region State
    private ApplicationStatus _status;
    // Total game-hours at time of submission
    private float _submittedAtHour;
    #endregion
    
    #region Constructor
    public QuestApplication(
        string questId,
        string[] partyMemberIds,
        string leaderId,
        bool isTemporaryParty,
        float partyStrength,
        float successChance,
        float partySubmittedAtHour
    )
    {
        _applicationId = Guid.NewGuid().ToString();
        _questId = questId;
        _partyMemberIds = partyMemberIds ?? Array.Empty<string>();
        _leaderId = leaderId;
        _isTemporaryParty = isTemporaryParty;
        _partyStrength = partyStrength;
        _successChance = Mathf.Clamp01(successChance);
        _status = ApplicationStatus.Pending;
        _submittedAtHour = partySubmittedAtHour;

        ValidateLeader();
    }
    #endregion
    
    #region Public Accessors
    public string ApplicationId => _applicationId;
    public string QuestId => _questId;
    public string[] PartyMemberIds => _partyMemberIds;
    public string LeaderId => _leaderId;
    public bool IsTemporaryParty => _isTemporaryParty;
    public float PartyStrength => _partyStrength;
    public float SuccessChance => _successChance;
    public ApplicationStatus Status => _status;
    public float SubmittedAtHour => _submittedAtHour;
    public int PartySize => _partyMemberIds?.Length ?? 0;
    public int EarlyFailureMarks => _earlyFailureMarks;
    public void AddEarlyFailureMark() => _earlyFailureMarks++;
    public void ResetEarlyFailureMarks() => _earlyFailureMarks = 0;
    #endregion
    
    #region Status Transitions
    // Transitions from Pending to Approved.
    // Called by QuestData.Dispatch().
    public void Approve()
    {
        if (_status != ApplicationStatus.Pending)
        {
            Debug.LogWarning($"[QuestApplication] {_applicationId} cannot be approved " +
                             $"— current status is {_status}.");
            return;
        }
        _status = ApplicationStatus.Approved;
    }
    
    // Transitions from Pending to Rejected.
    // Called when another application on the same quest is approved, or the guild manager manually rejects.
    public void Reject()
    {
        if (_status != ApplicationStatus.Pending)
        {
            Debug.LogWarning($"[QuestApplication] {_applicationId} cannot be rejected " +
                             $"— current status is {_status}.");
            return;
        }
        _status = ApplicationStatus.Rejected;
    }
    #endregion
    
    #region Queries
    // Returns true if the given adventurer ID is part of this application's party.
    public bool ContainsMember(string adventurerId)
    {
        if (_partyMemberIds == null || string.IsNullOrEmpty(adventurerId))
            return false;
        return _partyMemberIds.Any(id => id == adventurerId);
    }
    #endregion
    
    #region Private Helpers
    // Warns if the designated leader is not actually present in the party member list.
    private void ValidateLeader()
    {
        if (string.IsNullOrEmpty(_leaderId))
            return;
        if (!ContainsMember(_leaderId))
            Debug.LogWarning($"[QuestApplication] Leader ID '{_leaderId}' is not in the " +
                             $"partyMemberIds array for application {_applicationId}.");
    }
    #endregion
}