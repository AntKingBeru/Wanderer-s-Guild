// A box on the floor the guild camera's focus point must stay inside (resize it as rooms are added); drawn as a gizmo.
using UnityEngine;

namespace WanderersGuild
{
    public class CameraBoundsArea : MonoBehaviour
    {
        [Tooltip("Width (x) and depth (z) of the walkable area, centred on this object. Height is ignored.")]
        [SerializeField] private Vector2 size = new(20f, 28f);

        // The point clamped inside the box (shrunk by padding on each side); y is kept.
        public Vector3 Clamp(Vector3 point, float padding)
        {
            var centre = transform.position;
            var halfX = Mathf.Max(0f, size.x * 0.5f - padding);
            var halfZ = Mathf.Max(0f, size.y * 0.5f - padding);

            point.x = Mathf.Clamp(point.x, centre.x - halfX, centre.x + halfX);
            point.z = Mathf.Clamp(point.z, centre.z - halfZ, centre.z + halfZ);
            return point;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.05f, size.y));
        }
    }
}