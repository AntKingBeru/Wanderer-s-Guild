// Debug view: draws every room's real grid cells (and doors) in Play mode, to check models sit on their footprints.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Gizmos only — visible in the Scene view, and in the Game view with Gizmos on. No effect on the game.
    public class FacilityFootprintGizmos : MonoBehaviour
    {
        [SerializeField] private Grid grid;
        [SerializeField] private Color footprintColor = new(0.2f, 0.9f, 1f, 0.9f);
        [SerializeField] private Color doorColor = new(1f, 0.7f, 0.2f, 0.9f);

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !grid || !FacilityManager.HasInstance)
                return;

            foreach (var room in FacilityManager.Instance.Rooms)
            {
                var size = FacilityFootprint.RotatedSize(room.Data.Size, room.Rotation);
                var min = CellToWorld(room.Origin);
                Gizmos.color = footprintColor;
                Gizmos.DrawWireCube(new Vector3(min.x + size.x * 0.5f, min.y + 0.05f, min.z + size.y * 0.5f),
                    new Vector3(size.x, 0.1f, size.y));

                Gizmos.color = doorColor;
                foreach (var door in room.Doors)
                {
                    var a = CellToWorld(door.A) + new Vector3(0.5f, 0.1f, 0.5f);
                    var b = CellToWorld(door.B) + new Vector3(0.5f, 0.1f, 0.5f);
                    Gizmos.DrawLine(a, b);
                    Gizmos.DrawLine((a + b) * 0.5f, (a + b) * 0.5f + FacilityFootprint.ToVector(door.Facing) * 0.7f);
                }
            }
        }

        private Vector3 CellToWorld(Vector2Int cell) => grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
    }
}