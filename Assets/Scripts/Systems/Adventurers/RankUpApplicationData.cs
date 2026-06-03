// Represents an adventurer's application for a rank-up quest.
// Unlike regular quest applications, rank-up applications bypass the board entirely - they appear directly in the reception desk applications list.
// Created automatically by AdventurerManager when an adventurer crosses a rank-up threshold.

using System;
using UnityEngine;

[System.Serializable]
public class RankUpApplicationData
{
    #region Identity
    private string _applicationId;
    private string _adventurerId;
    private QuestRank _currentRank;
    private QuestRank _targetRank;
    private QuestCategory _questCategory;
    private float _durationHours;
    #endregion

    #region State
    private ApplicationStatus _status;
    private float _submittedAtHour;
    private float _successChance;
    private float _startHour = -1f;
    private float _endHour = -1f;
    #endregion

    #region Constructor
    public RankUpApplicationData(string adventurerId, QuestRank currentRank, QuestCategory questCategory,
        float durationHours, float successChance, float submittedAtHour)
    {
        _applicationId = System.Guid.NewGuid().ToString();
        _adventurerId = adventurerId;
        _currentRank = currentRank;
        _targetRank = (QuestRank)Mathf.Min((int)currentRank + 1, Enum.GetValues(typeof(QuestRank)).Length - 1);
        _questCategory = questCategory;
        _durationHours = durationHours;
        _successChance = Mathf.Clamp01(successChance);
        _status = ApplicationStatus.Pending;
        _submittedAtHour = submittedAtHour;
    }
    #endregion

    #region Public Accessors
    public string ApplicationId => _applicationId;
    public string AdventurerId => _adventurerId;
    public QuestRank CurrentRank => _currentRank;
    public QuestRank TargetRank => _targetRank;
    public QuestCategory QuestCategory => _questCategory;
    public float DurationHours => _durationHours;
    public ApplicationStatus Status => _status;
    public float SubmittedAtHour => _submittedAtHour;
    public float StartHour => _startHour;
    public float EndHour => _endHour;
    public float SuccessChance => _successChance;
    
    public bool IsDeadLinePassed(float currentGameHour) 
        => _endHour > 0f && currentGameHour >= _endHour;
    #endregion

    #region State Transitions
    public bool Approve(float currentGameHour)
    {
        if (_status != ApplicationStatus.Pending)
        {
            Debug.LogWarning($"[RankUpApplicationData] {_applicationId} cannot be approved - status is {_status}).");
            return false;
        }
        _status = ApplicationStatus.Approved;
        _startHour = currentGameHour;
        _endHour = currentGameHour + _durationHours;
        return true;
    }

    public bool Reject()
    {
        if (_status != ApplicationStatus.Pending)
        {
            Debug.LogWarning($"[RankUpApplicationData] {_applicationId} cannot be rejected - status is {_status}).");
            return false;
        }
        _status = ApplicationStatus.Rejected;
        return true;
    }
    #endregion
}