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
    private List<string> _memberIds;
    private bool _isTemporary;
    private int _questsCompletedTogether;
    #endregion
    
    #region Quest History
    private int _totalQuests;
    private int _totalSuccesses;
    private int _totalFailures;
    private int _consecutiveFailures;
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
            Debug.LogWarning($"[PartyData] Leader '{_leaderId}' is not in the initial member list. " +
                             "Adding automatically.");
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
    public int TotalQuests => _totalQuests;
    public int TotalSuccesses => _totalSuccesses;
    public int TotalFailures => _totalFailures;
    public int ConsecutiveFailures => _consecutiveFailures;
    
    public float SuccessRate => _totalQuests > 0 ?
        (float)_totalSuccesses / _totalQuests
        : 0f;
    
    public bool ContainsMember(string adventurerId) 
        => _memberIds.Contains(adventurerId);
    #endregion
    
    #region Membership
    public bool AddMember(string adventurerId)
    {
        if (_memberIds.Contains(adventurerId))
        {
            Debug.LogWarning($"[PartyData] '{adventurerId}' is already in the party {_partyId}.");
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
            Debug.LogWarning($"[PartyData] Leader left party {_partyId}." +
                             $"New leader is {_leaderId}.");
        }
        return true;
    }

    public bool SetLeader(string adventurerId)
    {
        if (!_memberIds.Contains(adventurerId))
        {
            Debug.LogWarning($"[PartyData] Cannot set leader to '{adventurerId}' " +
                             $"because they are not in the party {_partyId}.");
            return false;
        }
        _leaderId = adventurerId;
        return true;
    }
    #endregion
    
    #region Quest History
    public void RecordSuccess()
    {
        _totalQuests++;
        _totalSuccesses++;
        _consecutiveFailures = 0;
        _questsCompletedTogether++;
    }

    public void RecordFailure()
    {
        _totalQuests++;
        _totalFailures++;
        _consecutiveFailures++;
        _questsCompletedTogether++;
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