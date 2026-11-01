// Marker on a room prefab: a 2-cell-wide door, placed at the doorway's center on the room's edge, facing out of the room.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: No cell to type — the importer derives the door's two cells from this marker's position and the room's size.
    public class FacilityConnector : MonoBehaviour
    {
        [Tooltip("Which way the door faces (out of the room). The door sits on that edge of the footprint.")]
        [SerializeField] private CardinalDirection facing = CardinalDirection.North;

        public CardinalDirection Facing => facing;

        // The door's first cell in the unrotated footprint, from the marker's local position (relative to the prefab
        // root): snapped onto the facing's edge and to the nearest 2-cell span along it.
        public static Vector2Int StartCell(Vector3 local, CardinalDirection facing, Vector2Int size)
        {
            var alongX = facing is CardinalDirection.North or CardinalDirection.South;
            var edgeLength = alongX ? size.x : size.y;
            var alongPosition = alongX ? local.x : local.z;
            var start = Mathf.Clamp(Mathf.RoundToInt(alongPosition - FacilityFootprint.DoorWidth * 0.5f), 0,
                                    Mathf.Max(0, edgeLength - FacilityFootprint.DoorWidth));

            return facing switch
            {
                CardinalDirection.North => new Vector2Int(start, size.y - 1),
                CardinalDirection.South => new Vector2Int(start, 0),
                CardinalDirection.East => new Vector2Int(size.x - 1, start),
                _ => new Vector2Int(0, start)
            };
        }

        // NOTE: Shows the 2-cell span the marker resolves to along its edge (snapped to whole cells), plus the facing.
        private void OnDrawGizmos()
        {
            var root = transform.root;
            var local = root.InverseTransformPoint(transform.position);
            var alongX = facing is CardinalDirection.North or CardinalDirection.South;

            var along = Mathf.RoundToInt((alongX ? local.x : local.z) - FacilityFootprint.DoorWidth * 0.5f) + FacilityFootprint.DoorWidth * 0.5f;
            var centre = alongX ? new Vector3(along, 0.05f, local.z) : new Vector3(local.x, 0.05f, along);
            var size = alongX ? new Vector3(FacilityFootprint.DoorWidth, 0.05f, 0.2f) : new Vector3(0.2f, 0.05f, FacilityFootprint.DoorWidth);

            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.9f);
            Gizmos.matrix = root.localToWorldMatrix;
            Gizmos.DrawWireCube(centre, size);
            Gizmos.DrawLine(centre, centre + FacilityFootprint.ToVector(facing) * 0.7f);
        }
    }
}