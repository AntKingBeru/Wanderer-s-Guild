// Singleton: raycasts the cursor each frame and dispatches hover/click to world IInteractable.

using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-60)]
public class WorldInteractionController : MonoSingleton<WorldInteractionController>
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private float maxDistance = 100f;
    
    [Header("Input Action References")]
    [Tooltip("Vector2 'Value' action bound to <Pointer>/position.")]
    [SerializeField] private InputActionReference pointerPosition;
    [Tooltip("Button action bound to <Mouse>/leftButton.")]
    [SerializeField] private InputActionReference interact;

    private IInteractable _hovered;
    
    private void OnEnable()
    {
        pointerPosition?.action?.Enable();
        if (interact?.action != null)
        {
            interact.action.performed += OnInteractPerformed;
            interact.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interact?.action != null)
            interact.action.performed -= OnInteractPerformed;
    }

    private void Update()
    {
        if (!cam)
            cam = Camera.main;
        if (!cam || pointerPosition?.action == null)
            return;

        if (ScreenManager.Exists && ScreenManager.Instance.HasOpenScreen)
        {
            SetHover(null);
            return;
        }

        var pointer = pointerPosition.action.ReadValue<Vector2>();
        var ray = _=cam.ScreenPointToRay(pointer);

        SetHover(Physics.Raycast(ray, out var hit, maxDistance, interactableLayers, QueryTriggerInteraction.Ignore)
            ? hit.collider.GetComponentInParent<IInteractable>()
            : null);
    }
    
    private void SetHover(IInteractable next)
    {
        if (ReferenceEquals(next, _hovered))
            return;
        _hovered?.OnHoverExit();
        _hovered = next;
        _hovered?.OnHoverEnter();
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext _)
    {
        if (ScreenManager.Exists && ScreenManager.Instance.HasOpenScreen)
            return;
        _hovered?.OnInteract();
    }
}