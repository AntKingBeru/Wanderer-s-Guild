// Model popup shown when the player clicks a request in the Reception Desk list.
// Displays full request details with an X button to dismiss and a create quest button to confirm and load the request into the form.

using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class RequestPopupUI : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI categoryLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    [SerializeField] private TextMeshProUGUI rankRangeLabel;
    [SerializeField] private TextMeshProUGUI maxRewardLabel;
    [SerializeField] private TextMeshProUGUI partyLimitLabel;
    [SerializeField] private TextMeshProUGUI timeLimitLabel;
    
    [Header("Buttons")]
    [Tooltip("X button in the top-right corner. Closes the popup without creating a quest.")]
    [SerializeField] private Button closeButton;
    
    [Tooltip("Create Quest button in the bottom-right corner. Confirms and loads the form.")]
    [SerializeField] private Button createButton;
    
    // Fired when the X button is pressed.
    public event Action OnCloseClicked;
    
    // Fired when Create Quest is confirmed. Carries the request to the form.
    public event Action<QuestRequest> OnCreateClicked;
    
    private QuestRequest _request;
    
    #region Lifecycle
    private void Awake()
    {
        closeButton?.onClick.AddListener(HandleClose);
        createButton?.onClick.AddListener(HandleCreate);
    }

    private void OnDestroy()
    {
        closeButton?.onClick.RemoveListener(HandleClose);
        createButton?.onClick.RemoveListener(HandleCreate);
    }
    #endregion
    
    #region Public API
    public void Show(QuestRequest request)
    {
        _request = request;
        gameObject.SetActive(true);

        nameLabel.text = request.RequestName;
        categoryLabel.text = $"Category: {request.Category}";
        descriptionLabel.text = request.Description;
        maxRewardLabel.text = $"Max Reward: {request.MaxReward}g";
        partyLimitLabel.text = $"Party Limit: {request.PartyLimit}";
        timeLimitLabel.text = $"Time Limit: {request.TimeLimitHours}h";
        
        // Build the rank range string from QuestConfig display names.
        if (QuestManager.Instance?.Config)
        {
            var minRank = request.GetMinAllowedRank();
            var maxRank = request.GetMaxAllowedRank();
            var minName = QuestManager.Instance.Config.GetRankDisplayName(minRank);
            var maxName = QuestManager.Instance.Config.GetRankDisplayName(maxRank);
            rankRangeLabel.text = minRank == maxRank
                ? $"Rank {minName}"
                : $"Rank {minName} - {maxName}";
        }
        else
        {
            rankRangeLabel.text = $"Base Rank: {request.BaseRank}";
        }
    }

    public void Hide()
    {
        _request = null;
        gameObject.SetActive(false);
    }
    #endregion
    
    #region Handlers
    private void HandleClose() 
        =>  OnCloseClicked?.Invoke();

    private void HandleCreate()
    {
        if (_request is { IsAvailable: true })
            OnCreateClicked?.Invoke(_request);
    }
    #endregion
}