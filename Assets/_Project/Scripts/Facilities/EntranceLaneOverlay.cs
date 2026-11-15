// Build mode's marker for the kept-clear strip in front of the Guild Hall's door: one tinted quad from the hall's front
// edge to the edge of the build area.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Covers exactly the cells EntranceLane.Blocks refuses (same center-within-width rule), so what's shown is what's
    // blocked. Uses the ghost material with a property block (like the build grid).
    public sealed class EntranceLaneOverlay
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Grid _grid;
        private readonly GameObject _root;

        public EntranceLaneOverlay(Grid grid, Transform parent)
        {
            _grid = grid;
            var settings = GameConfig.Instance.BuildMode;

            _root = new GameObject("Entrance Lane");
            _root.transform.SetParent(parent, false);
            _root.AddComponent<MeshFilter>().sharedMesh = CreateQuad();

            var renderer = _root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = settings.GhostMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            MaterialPropertyBlock block = new();
            block.SetColor(BaseColorId, settings.EntranceLaneColor);
            block.SetColor(ColorId, settings.EntranceLaneColor);
            renderer.SetPropertyBlock(block);

            _root.SetActive(false);
        }

        public void Show(EntranceLane lane, RectInt area, float floorHeight)
        {
            if (_grid == null || !TryCellRect(lane, area, out var min, out var max))
            {
                Hide();
                return;
            }

            var a = ToWorld(min);
            var b = ToWorld(max);
            _root.transform.position = new Vector3(Mathf.Min(a.x, b.x), floorHeight + 0.02f, Mathf.Min(a.z, b.z));
            _root.transform.localScale = new Vector3(Mathf.Abs(b.x - a.x), 1f, Mathf.Abs(b.z - a.z));
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        public void Destroy() => Object.Destroy(_root);

        // NOTE: Across: the cells whose center is strictly within half the width (i + 0.5 in (c − h, c + h)). Along: from the
        // front line out to the build area's edge. min/max are cell-space corners (max exclusive).
        private static bool TryCellRect(EntranceLane lane, RectInt area, out Vector2 min, out Vector2 max)
        {
            min = max = default;
            if (!lane.IsValid)
                return false;

            var acrossMin = Mathf.FloorToInt(lane.Centre - lane.HalfWidth - 0.5f) + 1;
            var acrossMax = Mathf.CeilToInt(lane.Centre + lane.HalfWidth - 0.5f);
            if (acrossMax <= acrossMin)
                return false;

            var alongX = lane.Outward.x != 0;
            var direction = alongX ? lane.Outward.x : lane.Outward.y;
            int alongMin, alongMax;
            if (direction > 0)
            {
                alongMin = lane.FrontLine;
                alongMax = alongX ? area.xMax : area.yMax;
            }
            else
            {
                alongMin = alongX ? area.xMin : area.yMin;
                alongMax = lane.FrontLine + 1;
            }

            if (alongMax <= alongMin)
                return false;

            min = alongX ? new Vector2(alongMin, acrossMin) : new Vector2(acrossMin, alongMin);
            max = alongX ? new Vector2(alongMax, acrossMax) : new Vector2(acrossMax, alongMax);
            return true;
        }

        private Vector3 ToWorld(Vector2 cell) => _grid.LocalToWorld(_grid.CellToLocalInterpolated(new Vector3(cell.x, cell.y, 0f)));

        // NOTE: A 1×1 quad on the floor (x/z), facing up; scaled to the strip.
        private static Mesh CreateQuad()
        {
            Mesh mesh = new()
            {
                name = "EntranceLane",
                vertices = new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 1f) },
                normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                triangles = new[] { 0, 2, 1, 1, 2, 3 }
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}