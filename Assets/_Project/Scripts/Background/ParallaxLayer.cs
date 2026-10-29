// Shifts a parallax layer with the pointer and the camera pan; deeper layers shift more, forward layers less.
using UnityEngine;

namespace WanderersGuild
{
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 = front-most (shifts least), 1 = back-most (shifts the most).")]
        [SerializeField, Range(0f, 1f)] private float depth = 0.5f;

        private ParallaxGroup _group;
        private Vector3 _restLocalPosition;
        private Vector3 _pointerOffset;
        private float _panFactorOverride = -1f; // NOTE: < 0 = use depth × Pan Strength.

        public float Depth => depth;
        public ParallaxGroup Group => _group;

        // Local position with no parallax applied (captured in Awake).
        public Vector3 RestLocalPosition => _restLocalPosition;

        private void Awake()
        {
            _restLocalPosition = transform.localPosition;
            _group = GetComponentInParent<ParallaxGroup>();

            if (!_group)
                Debug.LogWarning($"[ParallaxLayer] '{name}' has no ParallaxGroup above it; pan parallax disabled.", this);
        }

        // Replaces depth × Pan Strength for camera-pan movement only (set by ParallaxPanJoint).
        public void SetPanFactorOverride(float factor) => _panFactorOverride = Mathf.Max(0f, factor);

        public void ClearPanFactorOverride() => _panFactorOverride = -1f;

        // NOTE: LateUpdate so the camera has already moved this frame (DOTween updates in Update); avoids pan jitter.
        private void LateUpdate()
        {
            var settings = GameConfig.Instance.Parallax;
            transform.localPosition = _restLocalPosition + SmoothedPointerOffset(settings) + PanOffset(settings);
        }

        // NOTE: The group's motion scales how far and how quickly the layer follows the pointer.
        private Vector3 SmoothedPointerOffset(ParallaxSettings settings)
        {
            if (!PointerTracker.HasInstance)
                return _pointerOffset;

            var motion = _group ? _group.Motion : ParallaxMotion.Background;
            var target = CalculateOffset(PointerTracker.Instance.NormalizedPosition, depth, settings) * settings.PointerStrength(motion);

            // NOTE: Exponential damping — frame-rate independent, unscaled (works while paused).
            var blend = 1f - Mathf.Exp(-settings.SmoothingFor(motion) * Time.unscaledDeltaTime);
            _pointerOffset = Vector3.Lerp(_pointerOffset, target, blend);
            return _pointerOffset;
        }

        // NOTE: Deliberately unsmoothed — the camera tween is already smooth; smoothing here would drift after the pan ends.
        private Vector3 PanOffset(ParallaxSettings settings)
        {
            if (!_group || !_group.TargetCamera)
                return Vector3.zero;

            var factor = _panFactorOverride >= 0f ? _panFactorOverride : depth * settings.PanStrength;
            var worldOffset = CalculatePanOffset(_group.TargetCamera.transform.position, _group.PanOrigin, 1f, factor);
            var parent = transform.parent;
            return parent != null ? parent.InverseTransformVector(worldOffset) : worldOffset;
        }

        // Pointer parallax math, shared by the runtime and the editor preview window.
        public static Vector3 CalculateOffset(Vector2 normalizedPointer, float depth, ParallaxSettings settings)
        {
            var scale = depth * (settings.MoveAgainstPointer ? -1f : 1f);
            return new Vector3(
                normalizedPointer.x * settings.MaxShift.x * scale,
                normalizedPointer.y * settings.MaxShift.y * scale,
                0f);
        }

        // World offset that makes the layer follow the camera by depth × strength; zero when the camera is at the origin.
        public static Vector3 CalculatePanOffset(Vector3 cameraPosition, Vector3 origin, float depth, float strength)
        {
            var offset = (cameraPosition - origin) * (depth * strength);
            offset.z = 0f;
            return offset;
        }
    }
}