using UnityEngine;
using UnityEngine.InputSystem;

namespace QuestSystem.UI
{
    /// <summary>
    /// Base class for clickable in-world 3-D objects that open a UI panel.
    /// Uses a simple raycast-from-camera approach.
    /// Override OpenPanel() / ClosePanel() in subclasses.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class WorldInteractable : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Bind to your 'Click' or 'Interact' InputAction (e.g. UI/Click).")]
        [SerializeField] private InputActionReference clickAction;
 
        [Header("Interaction")]
        [Tooltip("Maximum distance from the camera for the raycast to register.")]
        [SerializeField] private float interactDistance = 20f;
 
        [Tooltip("Layer(s) this interactable's collider lives on. " +
                 "Create a dedicated layer (e.g. 'Interactable') and assign it here " +
                 "AND on the GameObject itself so the raycast only hits interactable.")]
        [SerializeField] private LayerMask interactLayer = ~0;
 
        [Tooltip("Child GameObject used as a hover highlight (e.g. an outline mesh, " +
                 "a glow quad, or a projector). Toggled on/off based on pointer proximity.")]
        [SerializeField] private GameObject highlightObject;
        
        [SerializeField] private Camera mainCamera;

        private bool IsOpen { get; set; }
 
        private void OnEnable()
        {
            if (!clickAction)
                return;
            clickAction.action.Enable();
            
            clickAction.action.performed += OnClickPerformed;
        }
 
        private void OnDisable()
        {
            if (!clickAction)
                return;
            
            clickAction.action.performed -= OnClickPerformed;
        }
 
        private void Update()
        {
            // Hover highlight — runs every frame regardless of input events
            if (highlightObject)
                highlightObject.SetActive(IsPointerOverThis());
        }
 
        private void OnClickPerformed(InputAction.CallbackContext ctx)
        {
            if (!IsPointerOverThis())
                return;
 
            if (IsOpen)
                ClosePanel();
            else
                OpenPanel();
        }
        
        /// <summary>
        /// Returns true if the current pointer position ray hits this object's collider.
        /// Uses Mouse.Current for pointer position (new Input System).
        /// </summary>
        private bool IsPointerOverThis()
        {
            if (!mainCamera)
                return false;
 
            var mouse = Mouse.current;
            if (mouse == null)
                return false;
 
            var screenPos = mouse.position.ReadValue();
            var ray = mainCamera.ScreenPointToRay(screenPos);
 
            if (Physics.Raycast(ray, out var hit, interactDistance, interactLayer))
                return hit.transform == transform || hit.transform.IsChildOf(transform);
 
            return false;
        }
        
        protected void SetOpen(bool open) => IsOpen = open;
        protected abstract void OpenPanel();
        protected abstract void ClosePanel();
    }
}