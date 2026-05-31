// One of the 10 slots on the guild board. Acts as a drop target for UnpostedQuestItemUI.
// On a valid drop, calls QuestManager.PostQuestToSlot and updates its display.
// Slot index is assigned at runtime by QuestBoardUI.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuestBoardUISlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")] [Tooltip("Card component shown when this slot is occupied by a quest.")] [SerializeField]
    private QuestCardUI questCard;

    [Tooltip("Visual shown when this slot it empty (border, placeholder icon, etc.).")] [SerializeField]
    private GameObject emptyVisual;

    [Tooltip("Image used to tint the slot background on hover to indicate it accepts drops.")] [SerializeField]
    private Image slotBackground;

    [Header("Colors")] [Tooltip("Normal background color when empty and not hovered.")] [SerializeField]
    private Color emptyColor = new Color(1f, 1f, 1f, 0.05f);

    [Tooltip("Background color while a drag is hovering over this empty slot.")] [SerializeField]
    private Color hoverColor = new Color(1f, 1f, 1f, 0.20f);

    // Zero-based index in the board's slot array, assigned by QuestBoardUI.
    public int SlotIndex { get; private set; }
    public QuestData OccupiedQuest { get; private set; }

    #region Initialization
    public void Initialize(int slotIndex)
    {
        SlotIndex = slotIndex;
        SetEmpty();
    }

    #endregion

    #region Drop Handler
    public void OnDrop(PointerEventData eventData)
    {
        if (OccupiedQuest != null)
            return;
        var dragged = eventData.pointerDrag
            ? eventData.pointerDrag.GetComponent<UnpostedQuestItemUI>()
            : null;
        if (dragged?.Quest == null)
            return;
        var success = QuestManager.Instance && QuestManager.Instance.PostQuestToSlot(dragged.Quest, SlotIndex);
        if (success)
        {
            SetOccupied(dragged.Quest);
            ResetHoverColor();
        }
    }

    #endregion

    #region Hover Feedback
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OccupiedQuest != null)
            return;
        if (slotBackground)
            slotBackground.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverColor();
    }

    #endregion

    #region State
    public void SetOccupied(QuestData quest)
    {
        OccupiedQuest = quest;
        questCard?.Populate(quest);
        emptyVisual?.SetActive(false);
        ResetHoverColor();
    }

    public void SetEmpty()
    {
        OccupiedQuest = null;
        questCard?.Clear();
        emptyVisual?.SetActive(true);
        ResetHoverColor();
    }

    // Refreshes the card when the quest's status changes.
    // Clears the slot if the quest has left the board (dispatched, expired, etc.).
    public void Refresh()
    {
        if (OccupiedQuest == null)
            return;
        var stillOnBoard = OccupiedQuest.Status is QuestStatus.Posted or QuestStatus.InProgress;
        if (!stillOnBoard)
        {
            SetEmpty();
            return;
        }
        questCard?.Populate(OccupiedQuest);
    }
    #endregion
    
    #region Private
    private void ResetHoverColor()
    {
        if (slotBackground)
            slotBackground.color = emptyColor;
    }
    #endregion
}