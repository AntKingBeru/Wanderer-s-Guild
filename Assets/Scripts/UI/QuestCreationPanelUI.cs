using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuestSystem.UI
{
    /// <summary>
    /// The middle 60% panel of the Quest Creator screen.
    /// Populated when the player hits "Create Quest" on a request pop-up.
    /// Fields displayed:
    ///   - Quest Name (read-only, from request)
    ///   - Category   (read-only, from request)
    ///   - Category Tags (small display under category)
    ///   - Rank Slider  [rankMin-1 ... rankMax+1, clamped to [0 ... globalMax]]
    ///   - Reward Slider [0 ... maxRewardGold]
    ///   - Points (calculated live)
    ///   - People Limit (read-only)
    ///   - Time Limit   (read-only)
    ///   - [Post Quest] button
    /// </summary>
    public class QuestCreationPanelUI : MonoBehaviour
    {
        [Header("Read-only display fields")]
        [SerializeField] private TextMeshProUGUI questNameText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI categoryTagsText;
        [SerializeField] private TextMeshProUGUI adventurerLimitText;
        [SerializeField] private TextMeshProUGUI timeLimitText;
        [SerializeField] private TextMeshProUGUI pointsText;
        
        [Header("Rank Slider")]
        [SerializeField] private Slider rankSlider;
        [SerializeField] private TextMeshProUGUI rankValueText;
 
        [Header("Reward Slider")]
        [SerializeField] private Slider rewardSlider;
        [SerializeField] private TextMeshProUGUI rewardValueText;
 
        [Header("Action")]
        [SerializeField] private Button postQuestButton;
        [SerializeField] private GameObject emptyStateLabel;
        
        private QuestRequest _activeRequest;
        private int _rankMin, _rankMax;
        
        private void Awake()
        {
            rankSlider.onValueChanged.AddListener(OnRankChanged);
            rewardSlider.onValueChanged.AddListener(OnRewardChanged);
            postQuestButton.onClick.AddListener(OnPostClicked);
 
            ShowEmpty();
        }
        
        /// <summary>Populate the panel with data from the chosen request.</summary>
        public void PopulateFromRequest(QuestRequest request)
        {
            _activeRequest = request;
            var globalMax = QuestManager.Instance ? QuestManager.Instance.GlobalMaxRank : 10;
 
            // Static fields
            questNameText.text = request.requestName;
            categoryText.text = request.category.ToString();
            adventurerLimitText.text = $"Party Size: {request.adventurerLimit}";
            timeLimitText.text = $"Time Limit: {request.timeLimitMinutes} min";
 
            // Category tags (small, read-only)
            var tags = QuestCategoryTags.GetTags(request.category);
            categoryTagsText.text = tags.Count > 0 ? "Tags: " + string.Join(", ", tags) : "";
 
            // Rank slider
            _rankMin = Mathf.Max(0, request.minRank - 1);
            _rankMax = Mathf.Min(globalMax, request.maxRank + 1);
            rankSlider.minValue = _rankMin;
            rankSlider.maxValue = _rankMax;
            rankSlider.wholeNumbers = true;
            rankSlider.value = Mathf.Clamp(request.minRank, _rankMin, _rankMax);
 
            // Reward slider
            rewardSlider.minValue = 0;
            rewardSlider.maxValue = request.maxGoldReward;
            rewardSlider.wholeNumbers = true;
            rewardSlider.value = request.maxGoldReward;
 
            RefreshPoints();
            SetActiveState(true);
        }
        
        public void ShowEmpty()
        {
            SetActiveState(false);
        }
        
        private void SetActiveState(bool active)
        {
            if (emptyStateLabel) emptyStateLabel.SetActive(!active);
            rankSlider.gameObject.SetActive(active);
            rewardSlider.gameObject.SetActive(active);
            postQuestButton.gameObject.SetActive(active);

            if (active)
                return;
            
            questNameText.text   = "";
            categoryText.text    = "";
            categoryTagsText.text = "";
            adventurerLimitText.text = "";
            timeLimitText.text   = "";
            pointsText.text      = "";
            rankValueText.text   = "";
            rewardValueText.text = "";
        }
 
        private void OnRankChanged(float value)
        {
            rankValueText.text = $"Rank: {(int)value}";
            RefreshPoints();
        }
 
        private void OnRewardChanged(float value)
        {
            rewardValueText.text = $"Reward: {(int)value} gold";
        }
 
        private void RefreshPoints()
        {
            if (!_activeRequest)
                return;
            
            var pts = QuestPointsCalculator.Calculate((int)rankSlider.value, _activeRequest.category);
            pointsText.text = $"Points: {pts}";
        }
 
        private void OnPostClicked()
        {
            if (!_activeRequest) return;
 
            try
            {
                QuestManager.Instance.CreateQuest(
                    _activeRequest,
                    (int)rankSlider.value,
                    (int)rewardSlider.value);
 
                ShowEmpty();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[QuestCreationPanelUI] Failed to create quest: {ex.Message}");
            }
        }
    }
}