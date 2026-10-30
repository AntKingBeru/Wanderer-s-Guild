// Marker on a room prefab: a door at one edge cell of the unrotated footprint, facing outward; drawn as a gizmo.
using UnityEngine;

namespace WanderersGuild
{
    public class FacilityConnector : MonoBehaviour
    {
        [Tooltip("Edge cell of the unrotated footprint (x = 0…width-1, y = 0…depth-1 along +Z).")]
        [SerializeField] private Vector2Int cell;

        [Tooltip("Which way the door faces (out of the room).")]
        [SerializeField] private CardinalDirection facing = CardinalDirection.North;

        public Vector2Int Cell => cell;
        public CardinalDirection Facing => facing;

        // NOTE: Drawn relative to the prefab root (the footprint's min corner), so it lines up with the room's grid cells.
        private void OnDrawGizmos()
        {
            var root = transform.root;
            var centre = root.TransformPoint(new Vector3(cell.x + 0.5f, 0.05f, cell.y + 0.5f));
            var direction = root.TransformDirection(FacilityFootprint.ToVector(facing));

            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(centre, new Vector3(0.9f, 0.05f, 0.9f));
            Gizmos.DrawLine(centre, centre + direction * 0.7f);
        }
    }
}