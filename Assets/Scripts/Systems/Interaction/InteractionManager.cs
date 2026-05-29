// Singleton that drives all world-space interaction: hover detection, highlighting, click-to-open-screen, and Camera action map suppression.
// Each frame it casts a ray from the camera through the mouse cursor.
// If it hits a collider on the Interactable layer that has a WorldInteractable ancestor, that prop is highlighted and marked as the hover target.
// On the Interact action (left-click), the hovered prop's screen open.
// Opening a screen:
// - Fires OnScreenOpened(ScreenType) → UI layer shows the correct canvas.
// - Disables the 'Camera' action map → camera controls stop responding.
// - Blocks further world ray casting.
// Closing a screen (called by the UI's close button):
// - Fires OnScreenClosed(ScreenType) → UI layer hides the canvas.
// - Re-enables the 'Camera' action map.
// Build mode and other external systems can block interaction entirely by calling SetInteractionEnabled(false).

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }
    
    #region Inspector
    [Header("References")]
    [Tooltip("The project's InputActionAsset (GameInput.inputactions). " +
             "Used to find and toggle the 'Camera' action map on screen open/close.")]
    [SerializeField] private InputActionAsset inputActionAsset;
    
    [Tooltip("Left-Click action from the Gameplay map. " +
             "Bind to: Mouse/Left Button")]
    [SerializeField] private InputActionReference interactAction;
    
    [Tooltip("Camera used to build the interaction ray. " +
             "Leave empty to fall back to Camera.main at runtime.")]
    [SerializeField] private Camera cam;
    
    [Header("Raycast")]
    [Tooltip("Layer mask that includes the 'Interactable' layer. " +
             "The ray only reports hits on these layers, ignoring everything else.")]
    [SerializeField] private LayerMask interactableLayer;
    
    [Tooltip("Maximum world-unit distance at which props register as interactable.")]
    [SerializeField, Min(1f)] private float interactionRange = 25f;
    
    [Header("Cursor")]
    [Tooltip("Optional cursor texture to show while hovering over an interactable prop. " +
             "Leave empty to keep the system default cursor.")]
    [SerializeField] private Texture2D hoverCursor;
    
    [Tooltip("Pixel coordinate within the hover cursor texture that acts as the click point.")]
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    #endregion
    
    #region Private State
    private WorldInteractable _hoveredInteractable;
    private bool _isScreenOpen;
    private ScreenType _currentScreenType;
    private bool _interactionEnabled = true;
    // Cached once in Awake to avoid string lookup every frame.
    private InputActionMap _cameraActionMap;
    #endregion
    
    #region Events
    // Fired when a screen opens. UI scripts subscribe to show the correct canvas.
    public event Action<ScreenType> OnScreenOpened;
    // Fired when the active screen closes. UI scripts subscribe to hide their canvas.
    public event Action<ScreenType> OnScreenClosed;
    #endregion
    
    #region Properties
    public bool IsScreenOpen => _isScreenOpen;
    public ScreenType CurrentScreenType => _currentScreenType;
    public bool InteractionEnabled => _interactionEnabled;
    #endregion
    
    #region Lifecycle
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Camera fallback
        if (!cam)
        {
            cam = Camera.main;
            if (!cam)
                Debug.LogError("[InteractionManager] No camera found. Assign one in the inspector " +
                                     "or ensure a Camera tagged 'MainCamera' exists in the scene.");
        }
        // Cache the Camera action map.
        // If it can't be found, screen open/close will still work; only camera suppression is affected
        if (inputActionAsset)
        {
            _cameraActionMap = inputActionAsset.FindActionMap("Camera", throwIfNotFound: false);
            if (_cameraActionMap == null)
                Debug.LogWarning("[InteractionManager] 'Camera' action map not found in the " +
                                 "InputActionAsset. Camera controls will not be suppressed on screen open.");
        }
        else
        {
            Debug.LogWarning("[InteractionManager] InputActionAsset not assigned. " +
                             "Camera map suppression will not function.");
        }
    }

    private void OnEnable()
    {
        if (!interactAction)
            return;
        interactAction.action.Enable();
        interactAction.action.performed += HandleInteract;
    }

    private void OnDisable()
    {
        if (!interactAction)
            return;
        interactAction.action.performed -= HandleInteract;
        interactAction.action.Disable();
        // Prevent props staying highlighted if this component is toggled off.
        ClearHover();
    }
    
    private void Update()
    {
        if (_isScreenOpen || !_interactionEnabled)
        {
            // If we were hovering, clear it now so no prop stays highlighted.
            if (_hoveredInteractable)
                ClearHover();
            return;
        }
        
        PerformHoverRaycast();
    }
    #endregion
    
    #region Hover
    private void PerformHoverRaycast()
    {
        if (!cam)
            return;
        // Guard: Mouse.current is null when no mouse device is connected.
        if (Mouse.current == null)
            return;
        
        var mousePos = Mouse.current.position.ReadValue();
        var ray = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
        if (Physics.Raycast(ray, out var hit, interactionRange, interactableLayer))
        {
            // GetComponentInParent handles the common case where the collider sits on a
            // child mesh object but WorldInteractable is on the prop's root.
            var interactable = hit.collider.GetComponentInParent<WorldInteractable>();
            if (interactable)
            {
                if (interactable != _hoveredInteractable)
                    SetHoveredInteractable(interactable);
                return;
            }
        }
        // Nothing valid was hit this frame.
        if (_hoveredInteractable)
            ClearHover();
    }
    
    private void SetHoveredInteractable(WorldInteractable interactable)
    {
        // Exit the previous hover before entering the new one.
        _hoveredInteractable?.OnHoverExit();

        _hoveredInteractable = interactable;
        _hoveredInteractable.OnHoverEnter();

        Cursor.SetCursor(hoverCursor, cursorHotspot, CursorMode.Auto);
    }
    
    private void ClearHover()
    {
        _hoveredInteractable?.OnHoverExit();
        _hoveredInteractable = null;

        // Restore the default system cursor.
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
    #endregion
    
    #region Input
    private void HandleInteract(InputAction.CallbackContext ctx)
    {
        // Screens block world-space interaction entirely.
        if (_isScreenOpen || !_interactionEnabled)
            return;
        if (!_hoveredInteractable)
            return;

        OpenScreen(_hoveredInteractable.ScreenType);
    }
    #endregion
    
    #region Screen Control
    // Opens a screen by type. Disables camera controls and notifies the UI layer.
    // Can be called directly by other systems if they need to open a screen without a physical prop click (e.g. a keyboard shortcut).
    public void OpenScreen(ScreenType screenType)
    {
        // Clear hover so no prop stays highlighted underneath the open screen.
        ClearHover();

        _isScreenOpen = true;
        _currentScreenType = screenType;

        _cameraActionMap?.Disable();
        
        OnScreenOpened?.Invoke(screenType);
    }
    // Closes the active screen. Re-enables camera controls and notifies the UI layer.
    // Called by the UI canvas's close button (or equivalent input).
    public void CloseScreen()
    {
        if (!_isScreenOpen)
            return;

        var closedType = _currentScreenType;
        _isScreenOpen = false;
        
        _cameraActionMap?.Enable();
        
        OnScreenClosed?.Invoke(closedType);
    }
    #endregion
    
    #region External Control
    // Enables or disables all world interaction. When disabled, existing hover is cleared immediately so no prop is left in a highlighted state.
    // Call with false when entering build mode; call with true when leaving it.
    public void SetInteractionEnabled(bool value)
    {
        _interactionEnabled = value;
        if (!value)
            ClearHover();
    }
    #endregion
}