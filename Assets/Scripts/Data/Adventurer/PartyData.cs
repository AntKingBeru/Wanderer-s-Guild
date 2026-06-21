// Represents a registered adventurer party - either temporary (formed for a single quest) or permanent.
// Tracks membership, leadership, and quest history statistics.
// AdventurerManager owns all PartyData instances and is the only caller of mutating methods.

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PartyData
{
    #region Identity
    private string _partyId;
    private string _leaderId;
    private readonly List<string> _memberIds;
    private bool _isTemporary;
    #endregion
    
    #region Quest History
    private int _questsCompletedTogether;
    private int _totalSuccesses;
    private int _totalFailures;
    #endregion
    
    #region Constructor
    public PartyData(string partyId, string leaderId, IEnumerable<string> initialMemberIds, bool isTemporary)
    {
        _partyId = partyId;
        _leaderId = leaderId;
        _memberIds = new List<string>(initialMemberIds);
        _isTemporary = isTemporary;

        if (!_memberIds.Contains(_leaderId))
        {
            Debug.LogWarning($"[PartyData] Leader '{_leaderId}' is not in the initial member list. Adding automatically.");
            _memberIds.Add(leaderId);
        }
    }
    #endregion
    
    #region Public Accessors
    public string PartyId => _partyId;
    public string LeaderId => _leaderId;
    public IReadOnlyList<string> MemberIds => _memberIds;
    public bool IsTemporary => _isTemporary;
    public int QuestsCompletedTogether => _questsCompletedTogether;
    public int TotalSuccesses => _totalSuccesses;
    public int TotalFailures => _totalFailures;
    public float SuccessRate => _questsCompletedTogether > 0
        ? (float)_totalSuccesses / _questsCompletedTogether
        : 0f;
    public bool ContainsMember(string adventurerId)
        => _memberIds.Contains(adventurerId);
    #endregion
    
    #region Membership
    public bool AddMember(string adventurerId)
    {
        if (_memberIds.Contains(adventurerId))
        {
            Debug.LogWarning($"[PartyData] '{adventurerId}' is already in party {_partyId}.");
            return false;
        }
        _memberIds.Add(adventurerId);
        return true;
    }

    public bool RemoveMember(string adventurerId)
    {
        if (!_memberIds.Remove(adventurerId))
            return false;
        if (_leaderId == adventurerId && _memberIds.Count > 0)
        {
            _leaderId = _memberIds[0];
            Debug.Log($"[PartyData] Leader left party {_partyId}. New leader is {_leaderId}.");
        }
        return true;
    }

    public bool SetLeader(string adventurerId)
    {
        if (!_memberIds.Contains(adventurerId))
        {
            Debug.LogWarning($"[PartyData] Cannot set leader to '{adventurerId}' — not a member of {_partyId}.");
            return false;
        }
        _leaderId = adventurerId;
        return true;
    }
    #endregion
    
    #region Quest History & Trial Outcome
    // Records one quest's outcome and, for temporary parties, evaluates whether the trial
    // period is over. Permanent parties just accumulate stats and always return Continue.
    // Trial rule: once MinTrialQuests is reached, an all-success or all-failure run decides
    // immediately; otherwise it runs until MaxTrialQuests, where majority rules (tie = Disband).
    public PartyTrialResult RecordQuestResult(bool success, PartyConfig config)
    {
        _questsCompletedTogether++;
        if (success) _totalSuccesses++; else _totalFailures++;

        if (!_isTemporary || _questsCompletedTogether < config.MinTrialQuests)
            return PartyTrialResult.Continue;

        if (_totalFailures == 0)
            return PartyTrialResult.Promote;
        if (_totalSuccesses == 0)
            return PartyTrialResult.Disband;

        if (_questsCompletedTogether < config.MaxTrialQuests)
            return PartyTrialResult.Continue;

        return _totalSuccesses > _totalFailures ? PartyTrialResult.Promote : PartyTrialResult.Disband;
    }
    #endregion
    
    #region Status
    public void MakePermanent()
    {
        if (!_isTemporary)
        {
            Debug.LogWarning($"[PartyData] Party {_partyId} is already permanent.");
            return;
        }
        _isTemporary = false;
    }
    #endregion
}