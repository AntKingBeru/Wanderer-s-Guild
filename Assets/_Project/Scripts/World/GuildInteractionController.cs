// Hover + click for anything interactable in the world (guild objects, adventurers): outline + name label; click → Interact.
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

        private InputAction _point;
        private InputAction _click;
        private WorldHoverLabel _label;
        private IWorldInteractable _hovered;

        // NOTE: Also off in build mode — clicks there place rooms, not open screens.
        private bool IsBlocked =>
            (panels && (panels.HasOpenPanel || panels.IsBusy)) ||
            (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy) ||
            (BuildModeController.HasInstance && BuildModeController.Instance.IsActive);

        private void OnEnable()
        {
            var bindings = GameConfig.Instance.InputBindings;
            _point = Enable(bindings.Point);
            _click = Enable(bindings.WorldClick);
        }

        private void Start()
        {
            _label = view ? WorldHoverLabel.Bind(view.DocumentRoot) : null;
            if (_label == null)
                Debug.LogError("[GuildInteractionController] Missing the world label in WorldView.uxml.", this);
        }

        private void OnDisable() => SetHovered(null);

        // NOTE: A hovered target can be destroyed (an adventurer leaving) — dropped here before it's touched.
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

        // NOTE: Nothing under UI (any pickable element — the HUD bar, the card) or outside the view (letterbox). The ray uses
        // the view's live size (the texture follows the canvas).
        private IWorldInteractable FindUnderPointer()
        {
            if (_point == null || !view || !worldCamera)
                return null;

            var screen = _point.ReadValue<Vector2>();
            if (view.IsPointerOverUI(screen) || !view.TryScreenToView(screen, out var pixel))
                return null;

            var size = view.RenderSize;
            var ray = worldCamera.ViewportPointToRay(new Vector3(pixel.x / size.x, 1f - pixel.y / size.y, 0f));

            var settings = GameConfig.Instance.Interaction;
            return Physics.Raycast(ray, out var hit, settings.MaxRayDistance, settings.InteractableMask, QueryTriggerInteraction.Collide)
                ? hit.collider.GetComponentInParent<IWorldInteractable>()
                : null;
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
            Vector2 pixel = new(viewport.x * size.x, (1f - viewport.y) * size.y - GameConfig.Instance.Interaction.LabelGap);
            _label.Show(_hovered.DisplayName, view.ViewToLocal(pixel));
        }

        // NOTE: Interface references bypass Unity's destroyed-object null — check through UnityEngine.Object.
        private static bool IsAlive(IWorldInteractable target) =>
            target as Object ?? target != null;

        private static InputAction Enable(InputActionReference reference)
        {
            var action = reference ? reference.action : null;
            action?.Enable();
            return action;
        }
    }
}