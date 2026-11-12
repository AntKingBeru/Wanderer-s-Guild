// Hover + click for anything interactable in the world (guild objects, adventurers — by ray; bedrooms — by floor cell):
// outline + name label; click → Interact.
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    // NOTE: Runs after the camera rig (LateUpdate, order 100), so rays and the label use this frame's final camera pose.
    [DefaultExecutionOrder(100)]
    public class GuildInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldViewPresenter view;
        [SerializeField] private GamePanelHost panels;
        [Tooltip("The facility grid (bedrooms are picked by their floor cell).")]
        [SerializeField] private Grid grid;

        private InputAction _point;
        private InputAction _click;
        private WorldHoverLabel _label;
        private BedroomPicker _bedrooms;
        private IWorldInteractable _hovered;

        // NOTE: Also off in build mode — clicks there place rooms, not open screens.
        private bool IsBlocked =>
            (panels != null && (panels.HasOpenPanel || panels.IsBusy)) ||
            (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy) ||
            (BuildModeController.HasInstance && BuildModeController.Instance.IsActive);

        private void OnEnable()
        {
            var bindings = GameConfig.Instance.InputBindings;
            _point = Enable(bindings.Point);
            _click = Enable(bindings.WorldClick);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityRemoved);
        }

        private void Start()
        {
            _bedrooms = new BedroomPicker(grid);
            _label = view != null ? WorldHoverLabel.Bind(view.DocumentRoot) : null;
            if (_label == null)
                Debug.LogError("[GuildInteractionController] Missing the world label in WorldView.uxml.", this);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityRemoved);
            SetHovered(null);
        }

        // NOTE: A hovered target can be destroyed (an adventurer leaving, a room demolished) — dropped before it's touched.
        private void LateUpdate()
        {
            if (!IsAlive(_hovered))
                _hovered = null;

            var target = IsBlocked ? null : FindUnderPointer();
            SetHovered(target);

            if (_hovered != null && _click != null && _click.WasPressedThisFrame())
            {
                var clicked = _hovered;
                SetHovered(null);
                clicked.Interact(panels);
                return;
            }

            UpdateLabel();
        }

        // NOTE: Nothing under UI (any pickable element — the HUD bar, the cards) or outside the view (letterbox). Colliders
        // (furniture, adventurers) win; only when the ray hits none is the bedroom under the pointer's floor cell used.
        private IWorldInteractable FindUnderPointer()
        {
            if (_point == null || view == null || worldCamera == null)
                return null;

            var screen = _point.ReadValue<Vector2>();
            if (view.IsPointerOverUI(screen) || !view.TryScreenToView(screen, out var pixel))
                return null;

            var size = view.RenderSize;
            var ray = worldCamera.ViewportPointToRay(new Vector3(pixel.x / size.x, 1f - pixel.y / size.y, 0f));

            var settings = GameConfig.Instance.Interaction;
            if (Physics.Raycast(ray, out var hit, settings.MaxRayDistance, settings.InteractableMask, QueryTriggerInteraction.Collide))
            {
                var hitTarget = hit.collider.GetComponentInParent<IWorldInteractable>();
                if (hitTarget != null)
                    return hitTarget;
            }

            return _bedrooms?.Find(worldCamera, view, screen);
        }

        private void SetHovered(IWorldInteractable target)
        {
            if (ReferenceEquals(target, _hovered))
                return;

            if (IsAlive(_hovered))
                _hovered.SetHighlighted(false);
            _hovered = target;
            _hovered?.SetHighlighted(true);

            if (_hovered == null)
                _label?.Hide();
        }

        // NOTE: Hidden when the anchor is behind the camera; otherwise centred above it with a small gap.
        private void UpdateLabel()
        {
            if (_hovered == null || _label == null)
                return;

            var viewport = worldCamera.WorldToViewportPoint(_hovered.LabelAnchor);
            if (viewport.z <= 0f)
            {
                _label.Hide();
                return;
            }

            var size = view.RenderSize;
            Vector2 pixel = new(viewport.x * size.x, (1f - viewport.y) * size.y - GameConfig.Instance.Interaction.LabelGap * view.PixelScale);
            _label.Show(_hovered.DisplayName, view.ViewToLocal(pixel));
        }

        // NOTE: A removed room's hover target is dropped (and its outline count forgotten — its views are gone).
        private void OnFacilityRemoved(PlacedFacility room)
        {
            if (_hovered is BedroomTarget target && target.Room == room)
            {
                _hovered = null;
                _label?.Hide();
            }
            RoomHighlights.Forget(room);
            _bedrooms?.Forget(room);
        }

        // NOTE: Interface references bypass Unity's destroyed-object null — Unity objects check through it; bedroom targets
        // check that their room still exists.
        private static bool IsAlive(IWorldInteractable target) => target switch
        {
            null => false,
            Object unityObject => unityObject,
            BedroomTarget bedroom => bedroom.IsValid,
            _ => true
        };

        private static InputAction Enable(InputActionReference reference)
        {
            var action = reference != null ? reference.action : null;
            action?.Enable();
            return action;
        }
    }
}