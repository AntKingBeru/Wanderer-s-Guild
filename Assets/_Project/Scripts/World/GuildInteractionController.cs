// Hover + click for guild objects: rays through the world view, outline + name label on hover, click opens its screen.
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
        private GuildInteractable _hovered;

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

        private void LateUpdate()
        {
            var target = IsBlocked ? null : FindUnderPointer();
            SetHovered(target);

            if (_hovered && _click != null && _click.WasPressedThisFrame())
            {
                var opens = _hovered.Opens;
                SetHovered(null);
                panels.Open(opens);
                return;
            }

            UpdateLabel();
        }

        // NOTE: Nothing under UI (any pickable element — the HUD bar, buttons) or outside the view (letterbox).
        private GuildInteractable FindUnderPointer()
        {
            if (_point == null || !view || !worldCamera)
                return null;

            var screen = _point.ReadValue<Vector2>();
            if (view.IsPointerOverUI(screen) || !view.TryScreenToView(screen, out var pixel))
                return null;

            var size = GameConfig.Instance.WorldView.RenderSize;
            var ray = worldCamera.ViewportPointToRay(new Vector3(pixel.x / size.x, 1f - pixel.y / size.y, 0f));

            var settings = GameConfig.Instance.Interaction;
            return Physics.Raycast(ray, out var hit, settings.MaxRayDistance, settings.InteractableMask, QueryTriggerInteraction.Collide)
                ? hit.collider.GetComponentInParent<GuildInteractable>()
                : null;
        }

        private void SetHovered(GuildInteractable target)
        {
            if (target == _hovered)
                return;

            if (_hovered)
                _hovered.SetHighlighted(false);
            _hovered = target;
            if (_hovered)
                _hovered.SetHighlighted(true);

            if (!_hovered)
                _label?.Hide();
        }

        // NOTE: Hidden when the anchor is behind the camera; otherwise centred above it with a small gap.
        private void UpdateLabel()
        {
            if (!_hovered || _label == null)
                return;

            var viewport = worldCamera.WorldToViewportPoint(_hovered.LabelAnchor);
            if (viewport.z <= 0f)
            {
                _label.Hide();
                return;
            }

            var size = GameConfig.Instance.WorldView.RenderSize;
            Vector2 pixel = new(viewport.x * size.x, (1f - viewport.y) * size.y - GameConfig.Instance.Interaction.LabelGap);
            _label.Show(_hovered.DisplayName, view.ViewToLocal(pixel));
        }

        private static InputAction Enable(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            action?.Enable();
            return action;
        }
    }
}