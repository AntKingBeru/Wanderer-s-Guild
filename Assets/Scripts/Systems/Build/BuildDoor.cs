// World-space door prop that is visible and clickable only in build mode.
// Highlights on hover (same MaterialPropertyBlock approach as WorldInteractable)
// and notifies BuildRadialMenuUI to open at screen position on click.
// Uses Observer: fires OnDoorClicked so the UI layer reacts without a direct reference here.
// Add this component to the door GameObject. Give it a collider and place it on
// the "BuildInteractable" layer (separate from "Interactable" to avoid confusion with normal props).

using UnityEngine;
using UnityEngine.InputSystem;

public class BuildDoor : MonoBehaviour
{
    #region Inspector
    [Header("Highlight")]
    [Tooltip("Renderers to tint on hover. Auto-collected from children if empty.")]
    [SerializeField] private Renderer[] renderers;

    [Tooltip("HDR emission color applied while hovering.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color hoverColor = new(0.2f, 1f, 0.4f, 1f);

    [Header("Interaction")]
    [Tooltip("Layer mask containing the 'BuildInteractable' layer.")]
    [SerializeField] private LayerMask buildLayerMask;

    [Tooltip("Camera used for the hover raycast. Falls back to Camera.main.")]
    [SerializeField] private Camera cam;

    [Tooltip("Max click/hover distance in world units.")]
    [SerializeField, Min(1f)] private float interactionRange = 25f;

    [Header("Input")]
    [Tooltip("Left-click action. Reuse the Gameplay/Interact action reference.")]
    [SerializeField] private InputActionReference interactAction;
    #endregion
    
    #region Private
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private MaterialPropertyBlock _mpb;
    private bool _isHovered;
    #endregion
    
    #region Lifecycle
    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        if (!cam)
            cam = Camera.main;
    }

    private void Start()
    {
        // Enable emission keyword on all material instances.
        foreach (var r in renderers)
        {
            if (!r) continue;
            foreach (var mat in r.materials)
                mat.EnableKeyword("_EMISSION");
        }

        SetHighlight(false);
        // Doors start invisible; BuildModeController shows/hides them.
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (interactAction?.action != null)
            interactAction.action.performed += HandleClick;
    }

    private void OnDisable()
    {
        if (interactAction?.action != null)
            interactAction.action.performed -= HandleClick;

        if (_isHovered)
            SetHighlight(false);
        _isHovered = false;
    }

    // Called every frame by BuildModeController (only while build mode is active).
    public void UpdateHover(Ray ray)
    {
        // Check if this door's collider is hit.
        var hit = Physics.Raycast(ray, out var hitInfo, interactionRange, buildLayerMask);
        var shouldHover = hit && hitInfo.collider.GetComponentInParent<BuildDoor>() == this;

        if (shouldHover == _isHovered)
            return;
        _isHovered = shouldHover;
        SetHighlight(_isHovered);
    }
    #endregion
    
    #region Input
    private void HandleClick(InputAction.CallbackContext ctx)
    {
        if (!_isHovered)
            return;
        if (!BuildManager.Instance || !BuildManager.Instance.BuildModeActive)
            return;
        // Pass screen-space mouse position so the UI can anchor the menu near the cursor.
        var screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        GameEventRelay.Instance.onDoorClicked?.Invoke(this, screenPos);
    }
    #endregion
    
    #region Highlight
    private void SetHighlight(bool active)
    {
        var color = active ? hoverColor : Color.black;
        foreach (var r in renderers)
        {
            if (!r)
                continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionColorId, color);
            r.SetPropertyBlock(_mpb);
        }
    }
    #endregion
}