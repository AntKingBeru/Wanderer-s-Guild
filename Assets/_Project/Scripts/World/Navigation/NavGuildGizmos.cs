// Debug view: draws the avatars' nav grid in Play mode — free cells, cells blocked by furniture, and the near-wall rings.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Gizmos only — Scene view (or Game view with Gizmos on). No effect on the game.
    public class NavGridGizmos : MonoBehaviour
    {
        [SerializeField] private Grid grid;
        [SerializeField] private bool showOpenCells = true;
        [SerializeField] private Color openColor = new(0.3f, 1f, 0.4f, 0.3f);
        [SerializeField] private Color nearColor = new(1f, 0.8f, 0.2f, 0.5f);
        [SerializeField] private Color blockedColor = new(1f, 0.25f, 0.2f, 0.7f);

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || grid == null || !AvatarManager.HasInstance)
                return;

            var navigator = AvatarManager.Instance.Navigator;
            var size = new Vector3(0.9f, 0.02f, 0.9f);
            foreach (var cell in navigator.Grid.Cells)
            {
                Color color;
                if (navigator.Clearance.IsBlocked(cell))
                    color = blockedColor;
                else
                {
                    var ring = navigator.Clearance.DistanceToEdge(cell);
                    if (ring == 0 && !showOpenCells)
                        continue;
                    color = ring == 0 ? openColor : Color.Lerp(nearColor, openColor, (ring - 1) * 0.5f);
                }

                var corner = grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
                Gizmos.color = color;
                Gizmos.DrawCube(new Vector3(corner.x + 0.5f, corner.y + 0.03f, corner.z + 0.5f), size);
            }
        }
    }
}