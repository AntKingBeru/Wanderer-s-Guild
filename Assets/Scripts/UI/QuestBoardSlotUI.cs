using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace QuestSystem.UI
{
    #region Draggable Quest Card

    /// <summary>
    /// Represents a single unposted quest in the left panel list.
    /// Implements drag-and-drop so the player can drag it onto a board slot.
    /// </summary>
    public class QuestCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Display")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI questNameText;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private CanvasGroup canvasGroup;
 
        public QuestData Quest { get; private set; }
        public Transform OriginalParent { get; set; }
 
        // Called by QuestBoardUI to wire in the drag-complete callback
        public event Action<QuestCardUI, BoardSlotUI> OnDroppedOnSlot;
 
        private Canvas _rootCanvas;
        private Vector2 _originalAnchoredPosition;
        private int _originalSiblingIndex;
        
        public void Bind(QuestData quest, Canvas rootCanvas)
        {
            Quest = quest;
            _rootCanvas = rootCanvas;
            questNameText.text = quest.QuestName;
            rankText.text = $"Rank {quest.Rank}";
            categoryText.text = quest.Category.ToString();
        }
        
        // ── Drag Handlers ─────────────────────────────────────────────────────────
        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalAnchoredPosition = rectTransform.anchoredPosition;
            _originalSiblingIndex = transform.GetSiblingIndex();
            OriginalParent    = transform.parent;
 
            // Re-parent to root canvas so we render on top of everything
            transform.SetParent(_rootCanvas.transform, true);
            transform.SetAsLastSibling();
 
            canvasGroup.alpha = 0.75f;
            // allow raycast to pass to slots beneath
            canvasGroup.blocksRaycasts = false;
        }
 
        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
        }
 
        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
 
            // Search hovered objects for an empty BoardSlotUI
            BoardSlotUI targetSlot = null;
            foreach (var go in eventData.hovered)
            {
                var slot = go.GetComponent<BoardSlotUI>();
                if (slot && slot.IsEmpty)
                {
                    targetSlot = slot;
                    break;
                }
            }
 
            if (targetSlot)
            {
                OnDroppedOnSlot?.Invoke(this, targetSlot);
                // QuestBoardUI will Destroy this card on success; don't snap back.
            }
            else
            {
                // Snap back to original position inside the left list
                transform.SetParent(OriginalParent, true);
                transform.SetSiblingIndex(_originalSiblingIndex);
                rectTransform.anchoredPosition = _originalAnchoredPosition;
            }
        }
    }

    #endregion
    
    #region Board Slot

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

    #endregion
}