// A place in a room prefab where an adventurer can be (seat, table, counter, bed); its forward arrow is the way they face.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Registers while enabled — a room's spots exist only while its model does (finished levels; never on scaffolds or
    // the build ghost, which strips scripts).
    public class ActivitySpot : MonoBehaviour
    {
        [SerializeField] private SpotKind kind = SpotKind.Seat;

        public SpotKind Kind => kind;
        public Vector3 Position => transform.position;

        // Where they look while here (the marker's forward, flattened).
        public Vector3 Facing
        {
            get
            {
                var forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            }
        }

        private void OnEnable() => ActivitySpotRegistry.Register(this);
        private void OnDisable() => ActivitySpotRegistry.Unregister(this);

        private void OnDrawGizmos()
        {
            Gizmos.color = kind switch
            {
                SpotKind.Seat => new Color(0.4f, 0.8f, 1f),
                SpotKind.Table => new Color(1f, 0.7f, 0.3f),
                SpotKind.Counter => new Color(0.9f, 0.4f, 0.9f),
                _ => new Color(0.6f, 1f, 0.6f)
            };
            var p = transform.position + Vector3.up * 0.05f;
            Gizmos.DrawWireSphere(p, 0.25f);
            Gizmos.DrawLine(p, p + Facing * 0.6f);
        }
    }
}