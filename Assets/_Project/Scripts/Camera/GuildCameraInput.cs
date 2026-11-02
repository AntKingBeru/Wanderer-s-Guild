// Reads the camera input actions (move, middle-drag, Q/E rotate, wheel zoom) and drives the rig; ignored while a panel is open.
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    public class GuildCameraInput : MonoBehaviour
    {
        [SerializeField] private GuildCameraRig rig;
        [SerializeField] private GamePanelHost panels;
        [SerializeField] private WorldViewPresenter view;

        private InputAction _move;
        private InputAction _drag;
        private InputAction _rotate;
        private InputAction _zoom;
        private InputAction _point;
        private Vector2 _lastDragPixel;
        private bool _isDragging;

        private bool IsBlocked =>
            (panels && (panels.HasOpenPanel || panels.IsBusy)) ||
            (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy);

        private void OnEnable()
        {
            var bindings = GameConfig.Instance.InputBindings;
            _move = Enable(bindings.CameraMove);
            _drag = Enable(bindings.CameraDrag);
            _rotate = Enable(bindings.CameraRotate);
            _zoom = Enable(bindings.CameraZoom);
            _point = Enable(bindings.Point);
            _isDragging = false;
        }

        private void Update()
        {
            if (!rig)
                return;
            if (IsBlocked)
            {
                _isDragging = false;
                return;
            }

            ReadMove();
            ReadRotate();
            ReadZoom();
            ReadDrag();
        }

        // NOTE: Slower when zoomed in (scaled by the current distance relative to the far distance).
        private void ReadMove()
        {
            if (_move == null)
                return;

            var input = _move.ReadValue<Vector2>();
            if (input.sqrMagnitude < 0.0001f)
                return;

            var settings = GameConfig.Instance.GuildCamera;
            var speed = settings.PanSpeed * rig.Distance / settings.FarDistance;
            rig.Pan(Vector2.ClampMagnitude(input, 1f) * speed * Time.unscaledDeltaTime);
        }

        private void ReadRotate()
        {
            if (_rotate == null || !_rotate.WasPressedThisFrame())
                return;

            var value = _rotate.ReadValue<float>();
            if (Mathf.Abs(value) > 0.01f)
                rig.Rotate(value > 0f ? 1 : -1);
        }

        // NOTE: Wheel values differ per platform (±1 or ±120 per notch) — only the sign is used: one notch, one step.
        // Over UI (the build catalog, the HUD) the wheel belongs to the UI, not the camera.
        private void ReadZoom()
        {
            if (_zoom == null)
                return;

            var value = _zoom.ReadValue<float>();
            if (Mathf.Abs(value) <= 0.01f)
                return;
            if (view && _point != null && view.IsPointerOverUI(_point.ReadValue<Vector2>()))
                return;

            rig.ZoomBy(value > 0f ? 1 : -1);
        }

        // NOTE: The first held frame only records the position (no jump); outside the view (letterbox) the drag stops.
        private void ReadDrag()
        {
            if (_drag == null || _point == null || !view || !_drag.IsPressed() || !view.TryScreenToView(_point.ReadValue<Vector2>(), out var pixel))
            {
                _isDragging = false;
                return;
            }

            if (_isDragging)
                rig.PanByViewPixels(pixel - _lastDragPixel);

            _lastDragPixel = pixel;
            _isDragging = true;
        }

        private static InputAction Enable(InputActionReference reference)
        {
            var action = reference ? reference.action : null;
            action?.Enable();
            return action;
        }
    }
}