// Moves a pupil inside its eye toward the cursor in whole sprite pixels; lives under the eyes' parallax layer.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Runs after ParallaxLayer.LateUpdate, so it aims from this frame's eye position.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PupilFollower : MonoBehaviour
    {
        [Tooltip("This pupil's rest centre, in sprite pixels from the bottom-left of its 129×112 canvas.")]
        [SerializeField] private Vector2 restCenterPixel;

        private SpriteRenderer _renderer;
        private Camera _camera;
        private Vector3 _restLocalPosition;
        private Vector2 _offsetPixels;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _restLocalPosition = transform.localPosition;

            var group = GetComponentInParent<ParallaxGroup>();
            _camera = group ? group.TargetCamera : Camera.main;

            if (!transform.parent)
                Debug.LogWarning($"[PupilFollower] '{name}' should be a child of the eyes' parallax layer.", this);
        }

        private void LateUpdate()
        {
            var eye = transform.parent;
            var sprite = _renderer.sprite;
            if (!eye || !sprite || !_camera || !PointerTracker.HasInstance)
                return;

            var settings = GameConfig.Instance.PupilFollow;
            var pixelsPerUnit = sprite.pixelsPerUnit;

            // NOTE: Each pupil aims from its own eye center, so a cursor between the eyes makes them cross inward.
            // Measured in the eye layer's local space (1 sprite pixel = 1 / PPU units) from the rest position, never
            // the current one, so the pupil can't chase itself.
            var eyeCentre = _restLocalPosition + (Vector3)((restCenterPixel - sprite.pivot) / pixelsPerUnit);
            var cursor = eye.InverseTransformPoint(CursorWorld(eye.position.z));
            var towardCursorPixels = (Vector2)(cursor - eyeCentre) * pixelsPerUnit;

            var target = TargetOffset(towardCursorPixels, settings);
            var blend = 1f - Mathf.Exp(-settings.Smoothing * Time.unscaledDeltaTime);
            _offsetPixels = Vector2.Lerp(_offsetPixels, target, blend);

            // NOTE: Smoothed first, then snapped to whole sprite pixels — crisp pixel art, no sub-pixel shimmer.
            Vector2 snapped = new(RoundSymmetric(_offsetPixels.x), RoundSymmetric(_offsetPixels.y));
            transform.localPosition = _restLocalPosition + (Vector3)(snapped / pixelsPerUnit);
        }

        // NOTE: Direction toward the cursor, scaled onto an ellipse of max offsets; full offset from Reach Pixels away.
        private static Vector2 TargetOffset(Vector2 towardCursorPixels, PupilFollowSettings settings)
        {
            var distance = towardCursorPixels.magnitude;
            if (distance < 0.001f)
                return Vector2.zero;

            var direction = towardCursorPixels / distance;
            var reach = Mathf.Clamp01(distance / settings.ReachPixels);
            return new Vector2(direction.x * settings.MaxOffsetPixels.x, direction.y * settings.MaxOffsetPixels.y) * reach;
        }

        // NOTE: Rounds .5 away from zero (±0.5 → ±1). Mathf.Round rounds .5 to even, which made mirrored offsets on the
        // two eyes round differently.
        private static float RoundSymmetric(float value) => Mathf.Sign(value) * Mathf.Floor(Mathf.Abs(value) + 0.5f);

        private Vector3 CursorWorld(float planeZ)
        {
            var screen = PointerTracker.Instance.ScreenPosition;
            var distance = planeZ - _camera.transform.position.z;
            return _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, distance));
        }
    }
}