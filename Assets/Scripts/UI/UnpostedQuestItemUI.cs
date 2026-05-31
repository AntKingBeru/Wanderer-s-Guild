// Draggable card in the Quest Board screen's unposted quests list.
// On drag, creates a visual ghost on the root canvas that follows the cursor.
// On successful drop, QuestManager fires OnUnpostedQuestsChanged which causes QuestBoardUI to rebuild the list, destroying this item via referred Destroy.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class UnpostedQuestItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
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
    private bool _isDragging;
    
    #region Lifecucle
    private void OnDestroy()
    {
        // If this item is destroyed while a drag is in progress (e.g. the unposted list rebuilds after a successful drop), ensure the ghost is cleaned up.
        DestroyGhost();
    }
    #endregion
    
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
        if (!_rootCanvas)
            return;
        
        // Fade the original and pass raycasts through so targets can receive events.
        _isDragging = true;
        canvasGroup.alpha = dragAlpha;
        canvasGroup.blocksRaycasts = false;
        
        // Measure BEFORE instantiation
        // rect.width/height give the actual rendered size set by the LayoutGroup.
        // We must read these from the SOURCE before Instantiate copies the stale RectTransform values,
        // because the clone's sizeDelta may arrive as zero when the VLG drove width through anchor-stretch rather than sizeDelta
        var sourceRect = GetComponent<RectTransform>();
        var renderedSize = new Vector2(sourceRect.rect.width, sourceRect.rect.height);
        
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
            ghostGroup.interactable = false;
        }
        
        // Fix anchor and size
        // The clone inherits the VLG-assigned anchors which only made sense inside the scroll content container.
        // Parented to the root canvas, those anchors produce a position near a corner and collapse the width to zero.
        // Reset to a corner anchor at canvas origin so that anchoredPosition equals the canvas-local cursor position directly (matching ScreenToLocalPointInRectangle).
        var ghostRect = _dragGhost.GetComponent<RectTransform>();
        ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
        ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.sizeDelta = renderedSize;
        
        PositionGhostAtCursor(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || !_dragGhost)
            return;
        PositionGhostAtCursor(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
        => CleanupDrag();

    public void OnPointerUp(PointerEventData eventData)
        => CleanupDrag();
    #endregion
    
    #region Helpers
    private void CleanupDrag()
    {
        if (!_isDragging)
            return;
        _isDragging = false;
        if (canvasGroup)
            canvasGroup.alpha = 1f;

        DestroyGhost();
    }

    private void DestroyGhost()
    {
        if (!_dragGhost)
            return;
        
        _dragGhost.SetActive(false);
        Destroy(_dragGhost);
        _dragGhost = null;
    }
    
    private void PositionGhostAtCursor(Vector2 screenPosition)
    {
        if (!_dragGhost || !_rootCanvas)
            return;

        // ScreenPointToLocalPointInRectangle converts the screen cursor position
        // into the canvas's local coordinate space (origin at canvas bottom-left
        // for a standard Screen Space Overlay canvas).
        // With ghost anchorMin = anchorMax = (0,0), anchoredPosition equals the
        // distance from the canvas origin to the ghost's pivot — which is exactly
        // the canvas-local cursor position, so the ghost center tracks the cursor.
        var uiCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)_rootCanvas.transform,
                screenPosition,
                uiCamera,
                out var worldPoint))
        {
            ((RectTransform)_dragGhost.transform).position = worldPoint;
        }
    }
    #endregion
}