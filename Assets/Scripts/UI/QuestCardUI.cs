using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace QuestSystem.UI
{
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
}