// The form in the Reception Desk's middle panel.
// Populated from a QuestRequest via LoadRequest().
// The player adjusts rank (±1 slider) and reward (0-max slider) before creating the quest.

using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class QuestFormUI : MonoBehaviour
{
    [Header("Form Label")]
    [Tooltip("Quest name. Large, bold.")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    
    [Tooltip("Quest category. Small.")]
    [SerializeField] private TextMeshProUGUI categoryLabel;
    
    [Tooltip("Quest description. Auto-resizes height to fit content.")]
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    
    [Tooltip("Shows the currently selected rank letter/name. Medium.")]
    [SerializeField] private TextMeshProUGUI rankValueLabel;
    
    [Tooltip("Shows the currently selected reward amount. Medium.")]
    [SerializeField] private TextMeshProUGUI rewardValueLabel;
    
    [Tooltip("Shows the maximum allowed party size. Medium.")]
    [SerializeField] private TextMeshProUGUI partySizeLabel;
    
    [Header("Sliders")]
    [Tooltip("Integer slider. Range is set at runtime to base rank ±1")]
    [SerializeField] private Slider rankSlider;
    
    [Tooltip("Integer slider. Range is set at runtime to 0 - maxReward")]
    [SerializeField] private Slider rewardSlider;
    
    [Header("Buttons")]
    [Tooltip("Bottom-right of the form. Creates the quest from the current slider values.")]
    [SerializeField] private Button createQuestButton;
    
    [Header("State")]
    [Tooltip("Root of the content. Shown when a request is loaded.")]
    [SerializeField] private GameObject formContainer;
    
    [Tooltip("Shown instead of the form when no request has been selected yet.")]
    [SerializeField] private GameObject emptyStateContainer;
    
    // Fires when the player confirms quest creation.
    // Carries the source request, chosen rank, and chosen reward.
    public event Action<QuestRequest, QuestRank, int> OnCreateQuestClicked;
    
    private QuestRequest _loadedRequest;
    
    #region Lifecycle
    private void Awake()
    {
        rankSlider?.onValueChanged.AddListener(HandleRankSliderChanged);
        rewardSlider.onValueChanged.AddListener(HandleRewardSliderChanged);
        createQuestButton?.onClick.AddListener(HandleCreateQuest);
    }

    private void OnDestroy()
    {
        rankSlider?.onValueChanged.RemoveListener(HandleRankSliderChanged);
        rewardSlider.onValueChanged.RemoveListener(HandleRewardSliderChanged);
        createQuestButton?.onClick.RemoveListener(HandleCreateQuest);
    }

    public void OnEnable()
        => RefreshStateVisibility();
    #endregion
    
    #region Public API
    // Populated the form from a request and configures both slider.
    public void LoadRequest(QuestRequest request)
    {
        _loadedRequest = request;

        nameLabel.text = request.RequestName;
        categoryLabel.text = request.Category.ToString();
        descriptionLabel.text = request.Description;
        partySizeLabel.text = $"Max Party: {request.PartyLimit}";
        // Rank slider: integer steps within the allowed ±1 window.
        rankSlider.wholeNumbers = true;
        rankSlider.minValue = (int)request.GetMinAllowedRank();
        rankSlider.maxValue = (int)request.GetMaxAllowedRank();
        rankSlider.value = (int)request.BaseRank;
        // Reward slider: 0 to the full request reward; default to maximum.
        rewardSlider.wholeNumbers = true;
        rewardSlider.minValue = 0;
        rewardSlider.maxValue = request.MaxReward;
        rewardSlider.value = request.MaxReward;
        
        // Trigger label updates for the initial values.
        HandleRankSliderChanged(rankSlider.value);
        HandleRewardSliderChanged(rewardSlider.value);
    }
    
    // Clears the form and shows the empty state prompt.
    public void Clear()
    {
        _loadedRequest = null;
        RefreshStateVisibility();
    }
    #endregion
    
    #region Slider Callbacks
    private void HandleRankSliderChanged(float value)
    {
        if (!QuestManager.Instance?.Config)
            return;
        var rank = (QuestRank)(int)value;
        rankValueLabel.text = $"Rank: {QuestManager.Instance.Config.GetRankDisplayName(rank)}";
    }

    private void HandleRewardSliderChanged(float value)
    {
        rewardValueLabel.text = $"Reward: {(int)value}g";
    }
    #endregion
    
    #region Button Handlers
    private void HandleCreateQuest()
    {
        if (_loadedRequest is not { IsAvailable: true })
            return;
        var chosenRank = (QuestRank)(int)rankSlider.value;
        var chosenReward = (int)rewardSlider.value;
        OnCreateQuestClicked?.Invoke(_loadedRequest, chosenRank, chosenReward);
    }
    #endregion
    
    #region State Visibility
    private void RefreshStateVisibility()
    {
        var hasRequest = _loadedRequest is { IsAvailable: true };
        formContainer?.SetActive(hasRequest);
        emptyStateContainer?.SetActive(!hasRequest);
    }
    #endregion
}