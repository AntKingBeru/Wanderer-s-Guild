using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
        [Tooltip("Name of the InputActionMap to disable while the panel is open " + 
                 "(e.g. \"Player\"). Leave blank to skip map toggling.")]
        [SerializeField] private string cameraActionMapName = "Camera";
 
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
        
        // Cooldown prevents the action callback that fires the same frame Enable() is called
        // (e.g. if the mouse is already held down) from immediately opening the panel.
        private bool _readyToReceiveClick;

        private bool IsOpen { get; set; }
 
        private void OnEnable()
        {
            // Will be set true next frame in Update
            _readyToReceiveClick = false;
            
            if (!clickAction)
                return;
            clickAction.action.Enable();
            
            clickAction.action.performed += OnClickStarted;
        }
 
        private void OnDisable()
        {
            if (!clickAction)
                return;
            
            clickAction.action.performed -= OnClickStarted;
        }

        private void Update()
        {
            // Allow click reception after one full frame has passed since enable,
            // so we never react to a button that was already held when we subscribed.
            if (!_readyToReceiveClick)
            {
                _readyToReceiveClick = true;
                return;
            }

            // Hover highlight — runs every frame regardless of input events
            if (highlightObject)
                highlightObject.SetActive(IsPointerOverThis());
        }

        private void OnClickStarted(InputAction.CallbackContext ctx)
        {
            if (!_readyToReceiveClick)
                return;

            if (IsOpen)
                return;
            
            if (IsPointerOverUI())
                return;

            if (!IsPointerOverThis())
                return;
            
            OpenPanel();
        }

        /// <summary>
        /// True if the pointer is currently over any UI element.
        /// </summary>
        private static bool IsPointerOverUI()
        {
            return EventSystem.current &&
                   EventSystem.current.IsPointerOverGameObject();
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

        protected void SetOpen(bool open)
        {
            IsOpen = open;
            SetCameraMapEnabled(!open);
            
            if (highlightObject && open)
                highlightObject.SetActive(false);
        }

        private void SetCameraMapEnabled(bool value)
        {
            if (string.IsNullOrEmpty(cameraActionMapName))
                return;
            if (clickAction)
                return;
            
            var asset = clickAction.action.actionMap?.asset;
            if (!asset)
                return;
            
            var map = asset.FindActionMap(cameraActionMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogWarning($"[WorldInteractable] Action map '{cameraActionMapName}' not found in asset '{asset.name}'.");
                return;
            }
            
            if (value)
                map.Enable();
            else
                map.Disable();
        }
        
        protected abstract void OpenPanel();
        protected abstract void ClosePanel();
        
        /// <summary>
        /// Subclasses must call this to properly close (re-enables camera map, etc.).
        /// </summary>
        public void RequestClose()
        {
            ClosePanel();
            SetOpen(false);
        }
    }
}