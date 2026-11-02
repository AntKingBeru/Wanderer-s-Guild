// Faint grid lines around the cursor in build mode: one line mesh built once, moved to the cursor's cell.
using UnityEngine;

namespace WanderersGuild
{
    public sealed class BuildGridOverlay
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Grid _grid;
        private readonly GameObject _root;
        private readonly int _radius;

        public BuildGridOverlay(Grid grid, Transform parent)
        {
            _grid = grid;
            var settings = GameConfig.Instance.BuildMode;
            _radius = settings.GridRadius;

            _root = new GameObject("Build Grid");
            _root.transform.SetParent(parent, false);
            _root.AddComponent<MeshFilter>().sharedMesh = CreateMesh(_radius);

            var renderer = _root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = settings.GhostMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            MaterialPropertyBlock block = new();
            block.SetColor(BaseColorId, settings.GridColor);
            block.SetColor(ColorId, settings.GridColor);
            renderer.SetPropertyBlock(block);

            _root.SetActive(false);
        }

        // Centres the grid on a cell (the mesh's min corner sits <c>radius</c> cells below it).
        public void Show(Vector2Int cell, float floorHeight)
        {
            var corner = _grid.CellToWorld(new Vector3Int(cell.x - _radius, cell.y - _radius, 0));
            _root.transform.position = new Vector3(corner.x, floorHeight + 0.01f, corner.z);
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        public void Destroy() => Object.Destroy(_root);

        // NOTE: (2r+2) lines along each axis covering (2r+1)² cells; one mesh, drawn as lines.
        private static Mesh CreateMesh(int radius)
        {
            var count = radius * 2 + 2;
            float length = radius * 2 + 1;
            var vertices = new Vector3[count * 4];
            var indices = new int[count * 4];

            for (var i = 0; i < count; i++)
            {
                var v = i * 4;
                vertices[v] = new Vector3(i, 0f, 0f);
                vertices[v + 1] = new Vector3(i, 0f, length);
                vertices[v + 2] = new Vector3(0f, 0f, i);
                vertices[v + 3] = new Vector3(length, 0f, i);
                for (var k = 0; k < 4; k++)
                    indices[v + k] = v + k;
            }

            Mesh mesh = new() { name = "BuildGrid", vertices = vertices };
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}