// Tooltip popup shown while hovering a slice in the radial build menu.
// Displays: room name, description, gold cost, and build time.
// Follows the cursor by repositioning each frame.
// Attach to a Canvas child. Call Show() / Hide() from BuildRadialMenuUI.

using UnityEngine;
using TMPro;

public class BuildTooltipUI : MonoBehaviour
{
    [Header("Labels")]
    [Tooltip("Room name label.")]
    [SerializeField] private TMP_Text nameLabel;

    [Tooltip("Room description label.")]
    [SerializeField] private TMP_Text descriptionLabel;

    [Tooltip("Gold cost label.")]
    [SerializeField] private TMP_Text costLabel;

    [Tooltip("Build time label.")]
    [SerializeField] private TMP_Text buildTimeLabel;

    [Header("Offset")]
    [Tooltip("Pixel offset from the cursor to the tooltip top-left corner.")]
    [SerializeField] private Vector2 cursorOffset = new(15f, -15f);

    private RectTransform _rt;
    private RectTransform _parentRt;
    private bool _visible;
    
    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _parentRt = transform.parent as RectTransform;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_visible) return;
        // Follow cursor each frame.
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRt,
                Input.mousePosition,
                null,
                out var localPoint))
        {
            _rt.anchoredPosition = localPoint + cursorOffset;
        }
    }
    
    // Populate and show the tooltip near the given screen position.
    public void Show(RoomDefinition room, Vector2 screenPos)
    {
        if (!room) return;

        if (nameLabel)
            nameLabel.text = room.RoomName;
        if (descriptionLabel)
            descriptionLabel.text = room.Description;
        if (costLabel)
            costLabel.text = $"{room.GoldCost} Gold";
        if (buildTimeLabel)
            buildTimeLabel.text = $"{room.BuildTimeHours:0.#}h";

        gameObject.SetActive(true);
        _visible = true;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _visible = false;
    }
}