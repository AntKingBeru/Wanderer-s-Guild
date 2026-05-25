using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace QuestSystem.UI
{
    /// <summary>
    /// One of the fixed board slots in the right panel.
    /// Starts empty; accepts a dropped QuestCardUI and shows quest info.
    /// </summary>
    public class BoardSlotUI : MonoBehaviour, IDropHandler
    {
        [Header("Empty State")]
        [SerializeField] private GameObject emptyDisplay;
 
        [Header("Filled State")]
        [SerializeField] private GameObject filledDisplay;
        [SerializeField] private TextMeshProUGUI slotQuestNameText;
        [SerializeField] private TextMeshProUGUI slotRankText;
        [SerializeField] private TextMeshProUGUI slotCategoryText;
        [SerializeField] private TextMeshProUGUI slotRewardText;
 
        public bool IsEmpty => PostedQuest == null;
        public QuestData PostedQuest { get; private set; }
        public int SlotIndex { get; set; }
 
        private void Awake()
        {
            SetEmpty();
        }

        // Required by IDropHandler — QuestBoardUI handles the actual posting logic
        // via the card's OnDroppedOnSlot event; this stub is just so Unity knows
        // this object participates in the drop system.
        public void OnDrop(PointerEventData eventData)
        {
            
        }
 
        public void SetEmpty()
        {
            PostedQuest = null;
            
            if (emptyDisplay)
                emptyDisplay.SetActive(true);
            
            if (filledDisplay)
                filledDisplay.SetActive(false);
        }
 
        public void Fill(QuestData quest)
        {
            PostedQuest = quest;
            if (emptyDisplay)
                emptyDisplay.SetActive(false);
            
            if (filledDisplay)
                filledDisplay.SetActive(true);
 
            if (slotQuestNameText)
                slotQuestNameText.text = quest.QuestName;
            
            if (slotRankText)
                slotRankText.text = $"Rank {quest.Rank}";
            
            if (slotCategoryText)
                slotCategoryText.text = quest.Category.ToString();
            
            if (slotRewardText)
                slotRewardText.text = $"{quest.GoldReward} gold";
        }
    }
}