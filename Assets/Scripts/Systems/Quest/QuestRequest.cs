// Runtime wrapper around a QuestRequestData ScriptableObject that has been drawn from the pool and made available at the reception desk.
// Tracks whether the request has been converted to a quest or expired due to age.
// QuestManager is the ONLY called of MarkExpired() and MarkConverted().

using System;
using UnityEngine;

[Serializable]
public class QuestRequest
{
    // The pool template this was drawn from
    private QuestRequestData _sourceData;
    // TimeManager.Instance.Day at draw time
    private int _dayDrawn;
    private bool _isExpired;
    private bool _isConverted;
    
    #region Constructor
    public QuestRequest(QuestRequestData sourceData, int dayDrawn)
    {
        if (!sourceData)
            Debug.LogError($"[QuestRequest] Constructed with a null QuestRequestData. " +
                           "This request will be non-functional.");
        
        _sourceData = sourceData;
        _dayDrawn = dayDrawn;
        _isExpired = false;
        _isConverted = false;
    }
    #endregion
    
    #region Accessors
    public QuestRequestData SourceData => _sourceData;
    public int DayDrawn => _dayDrawn;
    public bool IsExpired => _isExpired;
    public bool IsConverted => _isConverted;
    
    // Available means neither expired nor already used to create a quest.
    public bool IsAvailable => !_isExpired && !_isConverted && _sourceData;
    
    // Pass-through so callers rarely need to reach through SourceData directly.
    // Null-conditional + null-coalescing guards against a missing source asset.
    public string RequestId => _sourceData?.RequestId;
    public string RequestName => _sourceData?.RequestName;
    public string Description => _sourceData?.Description;
    public string Location => _sourceData?.Location;
    public QuestCategory Category => _sourceData?.Category ?? QuestCategory.Combat;
    public QuestRank BaseRank => _sourceData?.BaseRank ?? QuestRank.F;
    public int MaxReward => _sourceData?.MaxReward ?? 0;
    public int PartyMin => _sourceData?.PartyMin ?? 1;
    public int PartyLimit => _sourceData?.PartyLimit ?? 1;
    public int TimeLimitHours => _sourceData?.TimeLimitHours ?? 24;

    public QuestRank GetMinAllowedRank() => _sourceData?.GetMinAllowedRank() ?? QuestRank.F;
    public QuestRank GetMaxAllowedRank() => _sourceData?.GetMaxAllowedRank() ?? QuestRank.F;
    #endregion
    
    #region State Transitions
    public void MarkExpired()
    {
        if (_isConverted)
        {
            Debug.LogWarning($"[QuestRequest] '{RequestName}' is already converted; " +
                             $"cannot also mark it expired.");
            return;
        }
        _isExpired = true;
    }

    public void MarkConverted()
    {
        if (_isExpired)
        {
            Debug.LogWarning($"[QuestRequest] '{RequestName}' has already expired; " +
                             $"cannot convert it to a quest.");
            return;
        }
        _isConverted = true;
    }
    #endregion
    
    #region Expiry Check
    // Returns true when this request has been sitting unconverted for too long.
    // Called by QuestManager during its daily expiry pass.
    public bool ShouldExpire(int currentDay, int expiryDays) 
        => _isConverted && !_isExpired && (currentDay - _dayDrawn) >= expiryDays;
    #endregion
}