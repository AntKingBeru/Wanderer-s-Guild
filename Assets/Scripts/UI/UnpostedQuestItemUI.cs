// Draggable card in the Quest Board screen's unposted quests list.
// On drag, creates a visual ghost on the root canvas that follows the cursor.
// On successful drop, QuestManager fires OnUnpostedQuestsChanged which causes QuestBoardUI to rebuild the list, destroying this item via referred Destroy.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class UnpostedQuestItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI rankLabel;
    [SerializeField] private TextMeshProUGUI categoryLabel;
    [SerializeField] private TextMeshProUGUI rewardLabel;
    
    [Header("Visuals")]
    [Tooltip("Colored bar or badge tinted to the quest's rank color.")]
    [SerializeField] private Image rankColorBar;
    
    [Header("Drag")]
    [Tooltip("Alpha of the original item while it is being dragged. " +
             "The ghost copy dragged by the cursor always stays fully opaque")]
    [SerializeField, Range(0f, 1f)] private float dragAlpha = 0.35f;
    
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    // The quest this item represents. Set by Populate().
    public QuestData Quest { get; private set; }
    private Canvas _rootCanvas;
    private GameObject _dragGhost;
    
    #region Public API
    // rootCanvas must be the topmost Canvas so the ghost renders above all panels.
    public void Populate(QuestData quest, Canvas rootCanvas)
    {
        Quest = quest;
        _rootCanvas = rootCanvas;

        nameLabel.text = quest.QuestName;
        categoryLabel.text = quest.Category.ToString();
        rewardLabel.text = $"{quest.PartyReward}g";

        if (QuestManager.Instance?.Config)
        {
            var config = QuestManager.Instance.Config.GetRankConfig(quest.Rank);
            rankLabel.text = config.DisplayName;
            if (rankColorBar)
                rankColorBar.color = config.CardColor;
        }
        else
        {
            rankLabel.text = quest.Rank.ToString();
        }
    }
    #endregion

    #region Drag Handlers
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Fade the original and pass raycasts through so targets can receive events.
        canvasGroup.alpha = dragAlpha;
        canvasGroup.blocksRaycasts = false;

        if (!_rootCanvas)
            return;
        
        // Create a full-opacity clone on the root canvas to act as the drag ghost.
        _dragGhost = Instantiate(gameObject, _rootCanvas.transform);
        
        // Disable the script on the ghost to prevent it from starting its own drag.
        var ghostScript = _dragGhost.GetComponent<UnpostedQuestItemUI>();
        if (ghostScript)
            ghostScript.enabled = false;
        
        var ghostGroup = _dragGhost.GetComponent<CanvasGroup>();
        if (ghostGroup)
        {
            ghostGroup.alpha = 1f;
            ghostGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragGhost)
            return;
        PositionGhostAtCursor(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore the original.
        // If the drop succeeded, the object will be destroyed at end of frame via QuestBoardUI's list rebuild.
        if (this && gameObject)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (_dragGhost)
        {
            Destroy(_dragGhost);
            _dragGhost = null;
        }
    }
    #endregion
    
    #region Helpers
    private void PositionGhostAtCursor(Vector2 screenPosition)
    {
        if (!_dragGhost || !_rootCanvas)
            return;
        
        // Convert screen position to the root canvas's local space.
        var uiCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
                screenPosition,
                uiCamera,
                out var localPoint))
        {
            ((RectTransform)_rootCanvas.transform).anchoredPosition = localPoint;
        }
    }
    #endregion
}