// Placed on any 3D prop the player can click to open a UI screen.
// Multiple props may share the same ScreenType - all of them open the same screen.
// Highlighting is done via MaterialPropertyBlock emission overrides.
// The emission keyword is enabled automatically on material instances in Start(), so ano manual material changes are required.
// The prop must use a URP Lit shader (or any shader that exposes _EmissionColor).
// Attach a collider to this GameObject(or its children) and set the layer ti 'Interactable' so InteractionManager's raycast can detect it.

using UnityEngine;

public class WorldInteractable : MonoBehaviour
{
    #region Inspector
    [Header("Interaction")]
    [Tooltip("Which UI screen opens when the player clicks this prop.")]
    [SerializeField] private ScreenType screenType;
    
    [Header("Highlight")]
    [Tooltip("Renderers to highlight on hover. If left empty, all Renderers on this " +
             "GameObject and its children are collected automatically in Awake.")]
    [SerializeField] private Renderer[] renderers;
    
    [Tooltip("Emission color applied while the player is hovering. " +
             "Enable HDR to use bloom-compatible intensities above 1.")]
    [ColorUsage(true, true) ]
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.7f, 1f, 1f);
    #endregion
    
    #region Private
    // Cached shader property ID. Computed once at class load, never per-frame.
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    // One block shared across all renderers; reading existing values before writing ensures we never clobber unrelated property on the same renderer.
    private MaterialPropertyBlock _propertyBlock;
    #endregion
    
    #region LifeCycle
    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        // Auto-collect if none were assigned. Include renderers on child objects, which is useful for props built from multiple meshes.
        if (renderers == null ||  renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        if (renderers.Length == 0)
            Debug.LogWarning($"[WorldInteractable] '{name}' has no Renderers. " +
                             $"Highlighting will have no visual effect.");
    }

    private void Start()
    {
        // Enable the _EMISSION keyword on per-instance materials so the property block color override actually takes effect at runtime.
        // renderer.materials creates instances on first access - subsequent calls return the same instances, so this is a one-time cost per renderer.
        foreach (var r in renderers)
        {
            if (!r)
                continue;
            foreach (var mat in r.materials)
                mat.EnableKeyword("_EMISSION");
        }
        // Always start with highlight off
        SetHighlight(false);
    }
    #endregion
    
    #region Public API
    // The screen this prop opens. Read by InteractionManager on click.
    public ScreenType ScreenType => screenType;
    // Called by InteractionManager when the mouse cursor enters this prop.
    public void OnHoverEnter()
        => SetHighlight(true);
    // Called by InteractionManager when the cursor leaves or interaction is blocked.
    public void OnHoverExit() 
        => SetHighlight(false);
    #endregion
    
    #region Highlight
    private void SetHighlight(bool active)
    {
        var target = active ? highlightColor : Color.black;

        foreach (var r in renderers)
        {
            if (!r)
                continue;
            // Read first so existing per-renderer overrides are preserved
            r.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorID, target);
            r.SetPropertyBlock(_propertyBlock);
        }
    }
    #endregion
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (!r)
                    continue;
                if (r.gameObject.layer != gameObject.layer)
                    Debug.LogWarning($"[WorldInteractable] '{name}': renderer '{r.name}' is on a " +
                                     $"different layer than the root. The collider layer must match " +
                                     $"the InteractionManager's Interactable layer mask.");
            }
        }
    }
#endif
}