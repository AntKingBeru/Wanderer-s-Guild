// Radial build menu shown when the player clicks a BuildDoor in build mode.
// Slices are created dynamically from BuildConfig.AvailableRooms at runtime (Builder pattern for slice construction).
// Hovering a slice shows a tooltip. Clicking a slice fires to BuildConfirmPopupUI.
// Subscribes to GameEventRelay.Instance.OnDoorClicked (static Observer event) so no scene reference is needed.
// Assign this component to the radial menu canvas root.
// Requires: one RadialSliceUI prefab per room (spawned as children of sliceContainer).

using System.Collections.Generic;
using UnityEngine;

public class BuildRadialMenuUI : MonoBehaviour
{
    #region Inspector
    [Header("References")]
    [Tooltip("Parent RectTransform that will contain the spawned slice UI items.")]
    [SerializeField] private RectTransform sliceContainer;

    [Tooltip("Prefab for a single radial slice. Must have a RadialSliceUI component.")]
    [SerializeField] private RadialSliceUI slicePrefab;

    [Tooltip("The tooltip popup shown on slice hover.")]
    [SerializeField] private BuildTooltipUI tooltip;

    [Tooltip("The confirmation popup shown on slice click.")]
    [SerializeField] private BuildConfirmPopupUI confirmPopup;

    [Header("Layout")]
    [Tooltip("Radius in pixels from the menu centre to the middle of each slice icon.")]
    [SerializeField, Min(50f)] private float radius = 120f;
    #endregion
    
    #region Private
    private readonly List<RadialSliceUI> _slices = new();
    private bool _isOpen;
    #endregion
    
    #region Lifecycle
    private void Awake()
    {
        gameObject.SetActive(false);
        tooltip?.Hide();
    }

    private void OnEnable()
    {
        GameEventRelay.Instance.OnDoorClicked.AddListener(HandleDoorClicked);
    }

    private void OnDisable()
    {
        GameEventRelay.Instance.OnDoorClicked.RemoveListener(HandleDoorClicked);
    }
    #endregion
    
    #region Open / Close
    private void HandleDoorClicked(BuildDoor door, Vector2 screenPos)
    {
        // If already open, close first (clicking a second door replaces the menu).
        Close();
        Open(screenPos);
    }

    private void Open(Vector2 screenPos)
    {
        if (!BuildManager.Instance?.Config)
            return;

        BuildSlices(BuildManager.Instance.Config.AvailableRooms);

        // Position menu root at the door's screen position.
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                null,
                out var localPoint))
        {
            ((RectTransform)transform).anchoredPosition = localPoint;
        }

        gameObject.SetActive(true);
        _isOpen = true;
    }

    // Called by the confirmation popup's cancel button, or on close.
    public void Close()
    {
        if (!_isOpen) return;
        tooltip?.Hide();
        gameObject.SetActive(false);
        _isOpen = false;
    }
    #endregion
    
    #region Slice Builder
    // Builder pattern: constructs and positions all slice UI elements from the room pool.
    private void BuildSlices(RoomDefinition[] rooms)
    {
        // Destroy old slices.
        foreach (var s in _slices)
            if (s) Destroy(s.gameObject);
        _slices.Clear();

        if (rooms == null || rooms.Length == 0)
            return;

        var angleStep = 360f / rooms.Length;

        for (var i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            var slice = Instantiate(slicePrefab, sliceContainer);
            var angle = i * angleStep * Mathf.Deg2Rad;
            // Position icon around a circle.
            var offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            ((RectTransform)slice.transform).anchoredPosition = offset;

            slice.Initialize(room, OnSliceHovered, OnSliceUnhovered, OnSliceClicked);
            _slices.Add(slice);
        }
    }
    #endregion
    
    #region Slice Callbacks
    private void OnSliceHovered(RoomDefinition room, Vector2 screenPos)
        => tooltip?.Show(room, screenPos);

    private void OnSliceUnhovered()
        => tooltip?.Hide();

    private void OnSliceClicked(RoomDefinition room)
    {
        tooltip?.Hide();
        // Hand off to confirm popup; pass a callback for the "back" path.
        confirmPopup?.Show(room, onConfirmed: () =>
            {
                BuildManager.Instance?.TryBuildRoom(room);
                Close();
            },
            onCancelled: () =>
            {
                // Player chose "back" — just re-show the radial menu (it was hidden by confirmPopup).
                gameObject.SetActive(true);
            });

        // Hide the radial menu while the confirmation popup is visible.
        gameObject.SetActive(false);
    }
    #endregion
}