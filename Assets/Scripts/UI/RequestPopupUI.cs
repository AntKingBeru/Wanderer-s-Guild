using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuestSystem.UI
{
    /// <summary>
    /// Pop-up shown after clicking a request in the list.
    /// Layout (top → bottom):
    ///   Request Name | Category | Description | Rank Range | Reward | People Limit | Time Limit
    ///   [top-right] Close button
    ///   [bottom-right] Create Quest button
    /// </summary>
    public class RequestPopupUI : MonoBehaviour
    {
        [Header("Info Fields")]
        [SerializeField] private TextMeshProUGUI requestNameText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI rankRangeText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI adventurerLimitText;
        [SerializeField] private TextMeshProUGUI timeLimitText;
 
        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button createQuestButton;
 
        // Fired when the player wants to start creating a quest from this request.
        public event Action<QuestRequest> OnCreateQuestClicked;
 
        private QuestRequest _currentRequest;
 
        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            createQuestButton.onClick.AddListener(() =>
            {
                OnCreateQuestClicked?.Invoke(_currentRequest);
                Hide();
            });
            gameObject.SetActive(false);
        }
 
        public void Show(QuestRequest request)
        {
            _currentRequest = request;
            var globalMax = QuestManager.Instance ? QuestManager.Instance.GlobalMaxRank : 10;
 
            requestNameText.text  = request.requestName;
            categoryText.text     = request.category.ToString();
            descriptionText.text  = request.description;
 
            var rMin = Mathf.Max(0, request.minRank - 1);
            var rMax = Mathf.Min(globalMax, request.maxRank + 1);
            rankRangeText.text = $"Rank: {rMin} – {rMax}";
            rewardText.text = $"Reward: 0 – {request.maxGoldReward} gold";
            adventurerLimitText.text = $"Party Size: {request.adventurerLimit}";
            timeLimitText.text = $"Time Limit: {request.timeLimitMinutes} min";
 
            gameObject.SetActive(true);
        }
 
        public void Hide() => gameObject.SetActive(false);
    }
}