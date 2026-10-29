// Guild camera: orbits a focus point on the floor — smoothed pan, 90° rotation steps, zoom that also tilts, bounds, pixel snap.
using UnityEngine;

namespace WanderersGuild
{
    [RequireComponent(typeof(Camera))]
    public class GuildCameraRig : MonoBehaviour
    {
        [Tooltip("The focus point must stay inside this box.")]
        [SerializeField] private CameraBoundsArea bounds;

        [Tooltip("Where the camera looks at first (on the floor).")]
        [SerializeField] private Vector3 startFocus = Vector3.zero;

        private Camera _camera;
        private GuildCameraSettings _settings;
        private Vector3 _focus, _focusTarget, _focusVelocity;
        private float _yaw, _yawTarget, _yawVelocity;
        private float _zoom, _zoomTarget, _zoomVelocity;

        // Current distance from the focus point (between far and near).
        public float Distance => GuildCameraPose.Distance(_zoom, _settings);

        // 0 at the far zoom, 1 at the near zoom.
        public float Zoom01 => _zoom;

        private static int RenderHeight => GameConfig.Instance.WorldView.RenderSize.y;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _settings = GameConfig.Instance.GuildCamera;

            _focus = _focusTarget = ClampFocus(startFocus, _settings);
            _zoom = _zoomTarget = _settings.StartZoom;
            _yaw = _yawTarget = Mathf.Repeat(_settings.StartYaw, 360f);
            Apply();
        }

        // The pose the camera starts in (also used by the editor preview, which passes the settings itself).
        public (Vector3 position, Quaternion rotation) StartPose(GuildCameraSettings settings, int renderHeight) =>
            GuildCameraPose.Compute(ClampFocus(startFocus, settings), Mathf.Repeat(settings.StartYaw, 360f),
                                    settings.StartZoom, settings, renderHeight);

        // Pans by a ground offset in camera-relative axes (x = right, y = forward), world units.
        public void Pan(Vector2 offset)
        {
            GroundAxes(out var right, out var forward);
            _focusTarget = ClampFocus(_focusTarget + right * offset.x + forward * offset.y, _settings);
        }

        // Drag-pans by a pointer move in view pixels (y down), so the ground follows the pointer.
        public void PanByViewPixels(Vector2 viewDelta)
        {
            var pixel = PixelSnap.PixelWorldSize(Distance, _settings.FieldOfView, RenderHeight);
            var pitch = GuildCameraPose.Pitch(_zoom, _settings);

            // NOTE: Vertical screen movement covers more ground on a tilted view (÷ sin(pitch)).
            var forward = viewDelta.y * pixel / Mathf.Max(0.1f, Mathf.Sin(pitch * Mathf.Deg2Rad));
            Pan(new Vector2(-viewDelta.x * pixel, forward));
        }

        // Rotates by whole steps (±1 = one rotation step, 90° by default).
        public void Rotate(int steps) => _yawTarget = Mathf.Repeat(_yawTarget + steps * _settings.RotationStep, 360f);

        // Zooms by whole steps (+ = closer).
        public void ZoomBy(int steps) => _zoomTarget = Mathf.Clamp01(_zoomTarget + steps * _settings.ZoomStep);

        // NOTE: Unscaled time — the camera keeps working while the game clock is paused.
        private void LateUpdate()
        {
            var dt = Time.unscaledDeltaTime;
            _focus = Vector3.SmoothDamp(_focus, _focusTarget, ref _focusVelocity, _settings.PanSmoothTime, Mathf.Infinity, dt);
            _yaw = Mathf.SmoothDampAngle(_yaw, _yawTarget, ref _yawVelocity, _settings.RotationSmoothTime, Mathf.Infinity, dt);
            _zoom = Mathf.SmoothDamp(_zoom, _zoomTarget, ref _zoomVelocity, _settings.ZoomSmoothTime, Mathf.Infinity, dt);
            Apply();
        }

        // NOTE: Snapping (inside GuildCameraPose) is applied after smoothing and never fed back into it.
        private void Apply()
        {
            var (position, rotation) = GuildCameraPose.Compute(_focus, _yaw, _zoom, _settings, RenderHeight);
            _camera.fieldOfView = _settings.FieldOfView;
            transform.SetPositionAndRotation(position, rotation);
        }

        private void GroundAxes(out Vector3 right, out Vector3 forward)
        {
            var yaw = Quaternion.Euler(0f, _yaw, 0f);
            right = yaw * Vector3.right;
            forward = yaw * Vector3.forward;
        }

        private Vector3 ClampFocus(Vector3 point, GuildCameraSettings settings) =>
            bounds ? bounds.Clamp(point, settings.BoundsPadding) : point;
    }
}