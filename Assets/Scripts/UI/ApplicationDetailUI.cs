// Overlay panel in the reception desk middle column.
// Opened by ApplicationsListUI when the player clicks a list item.
// Handles both regular quest applications and rank-up applications via two sub-panels that swap visibility.
// Approve/Decline buttons route through AdventurerManager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ApplicationDetailUI : MonoBehaviour
{
    #region Common Controls
    [Header("Common")]
    [SerializeField] private Button approveButton;
    [SerializeField] private Button declineButton;
    
    [Header("Header")]
    [SerializeField] private TMP_Text panelTitleLabel;
    #endregion
    
    #region Regular QuestPanel
    [Header("Regular Quest Panel")]
    [Tooltip("Root of the regular quest sub-panel. Enabled for regular applications.")]
    [SerializeField] private GameObject regularPanel;
    [SerializeField] private TMP_Text regularQuestNameLabel;
    [SerializeField] private TMP_Text regularRankLabel;
    [SerializeField] private TMP_Text regularCategoryLabel;
    [SerializeField] private TMP_Text regularGuildRewardLabel;
    [SerializeField] private TMP_Text regularAdventurerRewardLabel;
    [SerializeField] private TMP_Text regularPartyLabel;
    [SerializeField] private TMP_Text regularSuccessChanceLabel;
    #endregion
    
    #region Rank-Up QuestPanel
    [Header("Rank-Up Panel")]
    [Tooltip("Root of the rank-up sub-panel. Enabled for rank-up applications.")]
    [SerializeField] private GameObject rankUpPanel;
    [SerializeField] private TMP_Text rankUpTitleLabel;
    [SerializeField] private TMP_Text rankUpAdventurerLabel;
    [SerializeField] private TMP_Text rankUpRankLabel;
    [SerializeField] private TMP_Text rankUpClassLabel;
    [SerializeField] private TMP_Text rankUpStatsLabel;
    [SerializeField] private TMP_Text rankUpCategoryLabel;
    [SerializeField] private TMP_Text rankUpDurationLabel;
    [SerializeField] private TMP_Text rankUpSuccessChanceLabel;
    #endregion
    
    #region Runtime State
    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    private QuestApplication _currentRegularApplication;
    private RankUpApplicationData _currentRankUpApplication;
    private bool _isShowingRankUp;
    #endregion
    
    #region Lifecycle
    private void Awake()
    {
        SetVisible(false);
        approveButton?.onClick.AddListener(HandleApprove);
        declineButton?.onClick.AddListener(HandleDecline);
    }
    #endregion
    
    #region Public API
    public void ShowRegular(QuestData quest, QuestApplication application)
    {
        _currentRegularApplication = application;
        _currentRankUpApplication = null;
        _isShowingRankUp = false;
        
        regularPanel?.SetActive(true);
        rankUpPanel?.SetActive(false);

        if (panelTitleLabel)
            panelTitleLabel.text = "Quest Application";
        
        if (regularQuestNameLabel)
            regularQuestNameLabel.text = quest?.QuestName ?? "—";
        if (regularRankLabel)
            regularRankLabel.text = quest?.Rank.ToString() ?? "—";
        if (regularCategoryLabel)
            regularCategoryLabel.text = quest?.Category.ToString() ?? "—";
        
        if (regularGuildRewardLabel && quest != null)
            regularGuildRewardLabel.text = $"{quest.GuildReward}g";

        if (regularAdventurerRewardLabel && quest != null && application != null)
        {
            var partyMember = quest.GetGoldPerMember(application.PartyMemberIds.Length);
            regularAdventurerRewardLabel.text = $"{partyMember}g each";
        }

        if (regularPartyLabel && application != null)
        {
            var leader = AdventurerManager.Instance?.GetAdventurer(application.LeaderId);
            var leaderName = leader?.Name ?? "Unknown";
            var memberCount = application.PartyMemberIds.Length;
            regularPartyLabel.text = memberCount > 1
                ? $"{leaderName} + {memberCount - 1} member{(memberCount > 2 ? "s" : "")}"
                : leaderName;
        }
        
        if (regularSuccessChanceLabel && application != null)
            regularSuccessChanceLabel.text = $"Success Chance: {application.SuccessChance * 100f:F0}%";
        
        SetVisible(true);
    }

    public void ShowRankUp(RankUpApplicationData application, AdventurerData adventurer)
    {
        _currentRankUpApplication = application;
        _currentRegularApplication = null;
        _isShowingRankUp = true;
        
        regularPanel?.SetActive(false);
        rankUpPanel?.SetActive(true);
        
        if (panelTitleLabel)
            panelTitleLabel.text = "Rank-Up Application";
        
        if (rankUpTitleLabel)
            rankUpTitleLabel.text = $"Rank-Up Quest [{application.TargetRank}]";
        
        if (rankUpAdventurerLabel && adventurer != null)
            rankUpAdventurerLabel.text = adventurer.Name;

        if (rankUpRankLabel && adventurer != null)
            rankUpRankLabel.text = $"{adventurer.Rank} \u2192 {application.TargetRank}";

        if (rankUpClassLabel && adventurer != null)
            rankUpClassLabel.text = adventurer.Class.ToString();

        if (rankUpStatsLabel && adventurer != null)
            rankUpStatsLabel.text =
                $"HP {adventurer.MaxHp:F0}   DMG {adventurer.Damage:F1}   SPD {adventurer.Speed:F1}";

        if (rankUpCategoryLabel)
            rankUpCategoryLabel.text = $"Type: {application.QuestCategory}";

        if (rankUpDurationLabel)
            rankUpDurationLabel.text = $"Duration: {FormatDuration(application.DurationHours)}";

        if (rankUpSuccessChanceLabel)
            rankUpSuccessChanceLabel.text = $"Success Chance: {application.SuccessChance * 100f:F0}%";

        SetVisible(true);
    }
    
    public void Hide() => SetVisible(false);
    #endregion
    
    #region Button Handlers
    private void HandleApprove()
    {
        bool success;
        if (_isShowingRankUp)
        {
            success = _currentRankUpApplication != null
                      && AdventurerManager.Instance
                      && AdventurerManager.Instance.ApproveRankUpApplication(
                          _currentRankUpApplication.ApplicationId);
        }
        else
        {
            success = _currentRegularApplication != null
                      && AdventurerManager.Instance
                      && AdventurerManager.Instance.ApproveQuestApplication(_currentRegularApplication);
        }

        if (success)
            Hide();
        else
            Debug.LogWarning("[ApplicationDetailUI] Approval failed — check manager logs.");
    }
    
    private void HandleDecline()
    {
        if (_isShowingRankUp && _currentRankUpApplication != null)
            AdventurerManager.Instance?.RejectRankUpApplication(_currentRankUpApplication.ApplicationId);
        else if (_currentRegularApplication != null)
            AdventurerManager.Instance?.RejectQuestApplication(_currentRegularApplication);

        Hide();
    }
    #endregion
    
    #region Helpers
    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    private static string FormatDuration(float hours)
        => hours < 24f ? $"{hours:F0}h" : $"{hours / 24f:F1}d";
    #endregion
}