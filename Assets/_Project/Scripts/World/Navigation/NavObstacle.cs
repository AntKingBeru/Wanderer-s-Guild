// Marks furniture avatars walk around: a floor rectangle (local X/Z) whose covered cells are blocked in the guild's nav grid.
// Put it on furniture prefabs; the "Fit To Renderers" context menu sizes it.
using UnityEngine;

namespace WanderersGuild
{
    [DisallowMultipleComponent]
    public class NavObstacle : MonoBehaviour
    {
        private const float MinSize = 0.05f;

        [Tooltip("Centre of the floor rectangle in local space (Y ignored).")]
        [SerializeField] private Vector3 center;

        [Tooltip("Width (local X) and depth (local Z) of the floor rectangle, in local units.")]
        [SerializeField] private Vector2 size = Vector2.one;

        private void OnEnable() => NavObstacleRegistry.Register(this);
        private void OnDisable() => NavObstacleRegistry.Unregister(this);

        private void OnValidate() => size = new Vector2(Mathf.Max(MinSize, size.x), Mathf.Max(MinSize, size.y));

        // NOTE: The test is done in the rectangle's own axes, so rotated rooms/furniture (and scale) are handled.
        // True if this world point (X/Z) lies inside the rectangle grown by padding (world units).
        public bool Covers(Vector3 point, float padding)
        {
            Axes(out var centre, out var halfX, out var halfZ);
            var offset = point - centre;
            offset.y = 0f;
            return Within(offset, halfX, padding) && Within(offset, halfZ, padding);
        }

        // The rectangle's world X/Z bounds (grown by padding) — which cells to test.
        public Rect FloorRect(float padding)
        {
            Axes(out var c, out var hx, out var hz);
            var extentX = Mathf.Abs(hx.x) + Mathf.Abs(hz.x) + Mathf.Max(0f, padding);
            var extentZ = Mathf.Abs(hx.z) + Mathf.Abs(hz.z) + Mathf.Max(0f, padding);
            return Rect.MinMaxRect(c.x - extentX, c.z - extentZ, c.x + extentX, c.z + extentZ);
        }

        // NOTE: Center + half-extent vectors on the floor, with this object's rotation and scale applied.
        private void Axes(out Vector3 centre, out Vector3 halfX, out Vector3 halfZ)
        {
            centre = transform.TransformPoint(center);
            halfX = transform.TransformVector(new Vector3(size.x * 0.5f, 0f, 0f));
            halfZ = transform.TransformVector(new Vector3(0f, 0f, size.y * 0.5f));
            halfX.y = 0f;
            halfZ.y = 0f;
        }

        private static bool Within(Vector3 offset, Vector3 half, float padding)
        {
            var length = half.magnitude;
            return length > 0.0001f && Mathf.Abs(Vector3.Dot(offset, half / length)) <= length + padding;
        }

#if UNITY_EDITOR
        // Editor: fits the rectangle around every child renderer's mesh bounds (on the local X/Z plane).
        [ContextMenu("Fit To Renderers")]
        private void FitToRenderers()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[NavObstacle] '{name}' has no renderers to fit.", this);
                return;
            }

            var min = new Vector3(float.MaxValue, 0f, float.MaxValue);
            var max = new Vector3(float.MinValue, 0f, float.MinValue);
            foreach (var target in renderers)
            {
                var bounds = target.localBounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var local = transform.InverseTransformPoint(target.transform.TransformPoint(corner));
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }

            UnityEditor.Undo.RecordObject(this, "Fit Nav Obstacle");
            center = new Vector3((min.x + max.x) * 0.5f, 0f, (min.z + max.z) * 0.5f);
            size = new Vector2(Mathf.Max(MinSize, max.x - min.x), Mathf.Max(MinSize, max.z - min.z));
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnDrawGizmosSelected()
        {
            Axes(out var c, out var hx, out var hz);
            var lift = Vector3.up * 0.02f;
            var a = c - hx - hz + lift;
            var b = c + hx - hz + lift;
            var d = c - hx + hz + lift;
            var e = c + hx + hz + lift;
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 1f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, e);
            Gizmos.DrawLine(e, d);
            Gizmos.DrawLine(d, a);
        }
#endif
    }
}