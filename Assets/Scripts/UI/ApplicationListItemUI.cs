// A single row in the reception desk applications list.
// Handles both regular quest applications and rank-up applications.
// Clicking the row fires OnItemClicked; ApplicationsListUI handles opening the detail view.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ApplicationListItemUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Narrow strip on the left edge, tinted to the relevant rank colour.")]
    [SerializeField] private Image rankBar;

    [Tooltip("Main line — quest name or 'Rank Up → [Rank]'.")]
    [SerializeField] private TMP_Text titleLabel;

    [Tooltip("Secondary line — applicant name or party summary.")]
    [SerializeField] private TMP_Text subtitleLabel;

    [Tooltip("Small badge label — 'Quest' or 'Rank Up'.")]
    [SerializeField] private TMP_Text typeLabel;

    [SerializeField] private Button button;
    
    #region Stored Application Data
    private QuestApplication _regularApplication;
    private RankUpApplicationData _rankUpApplication;
    private QuestData _quest;
    private bool _isRankUp;

    public event Action<ApplicationListItemUI> OnItemClicked;
    #endregion
    
    #region Public Accessors
    public QuestApplication RegularApplication => _regularApplication;
    public RankUpApplicationData RankUpApplication => _rankUpApplication;
    public QuestData Quest => _quest;
    public bool IsRankUp => _isRankUp;
    #endregion
    
    #region Initialization
    public void InitializeRegular(QuestApplication application, QuestData quest, QuestConfig questConfig)
    {
        _regularApplication = application;
        _quest = quest;
        _isRankUp = false;

        if (typeLabel)
            typeLabel.text = "Quest";
        if (titleLabel)
            titleLabel.text = quest?.QuestName ?? "-";
        if (subtitleLabel)
        {
            var leader = AdventurerManager.Instance?.GetAdventurer(application.LeaderId);
            var leaderName = leader?.Name ?? "Unknown";
            var memberCount = application.PartyMemberIds.Length;
            subtitleLabel.text = memberCount > 1
                ? $"{leaderName} + {memberCount}"
                : leaderName;
        }

        if (rankBar && questConfig && quest != null)
            rankBar.color = questConfig.GetRankConfig(quest.Rank).CardColor;

        button?.onClick.AddListener(HandleClick);
    }

    public void InitializeRankUp(RankUpApplicationData application, QuestConfig questConfig)
    {
        _rankUpApplication = application;
        _isRankUp = true;
        
        if (typeLabel)
            typeLabel.text = "Rank Up";
        if (titleLabel)
            titleLabel.text = $"Rank Up \u2192 {application.TargetRank}";
        if (subtitleLabel)
        {
            var adventurer = AdventurerManager.Instance?.GetAdventurer(application.AdventurerId);
            subtitleLabel.text = adventurer?.Name ?? "Unknown";
        }
        
        if (rankBar && questConfig)
            rankBar.color = questConfig.GetRankConfig(application.TargetRank).CardColor;
        
        button?.onClick.AddListener(HandleClick);
    }
    #endregion
    
    #region Events
    private void HandleClick() => OnItemClicked?.Invoke(this);
    
    private void OnDestroy() => button?.onClick.RemoveListener(HandleClick); 
    #endregion
}