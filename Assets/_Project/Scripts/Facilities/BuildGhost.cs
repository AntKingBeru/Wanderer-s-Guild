// The see-through preview of the room being placed: its level-1 model (scripts and colliders stripped) plus a footprint tint.
using UnityEngine;

namespace WanderersGuild
{
    public sealed class BuildGhost
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Grid _grid;
        private readonly Transform _parent;
        private readonly MaterialPropertyBlock _block = new();
        private GameObject _root;
        private GameObject _model;
        private GameObject _footprint;
        private Renderer[] _renderers;

        public BuildGhost(Grid grid, Transform parent)
        {
            _grid = grid;
            _parent = parent;
        }

        // NOTE: Instantiated under an inactive root and stripped immediately, so none of the prefab's scripts ever wake up.
        public void Build(FacilityData data)
        {
            Clear();
            var material = GameConfig.Instance.BuildMode.GhostMaterial;

            _root = new GameObject($"Build Ghost ({data.DisplayName})");
            _root.SetActive(false);
            _root.transform.SetParent(_parent, false);

            var prefab = data.PrefabFor(1);
            _model = prefab ? Object.Instantiate(prefab, _root.transform) : CreateBox(data.Size);
            Strip(_model);

            _footprint = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(_footprint.GetComponent<Collider>());
            _footprint.transform.SetParent(_root.transform, false);
            _footprint.name = "Footprint";

            _renderers = _root.GetComponentsInChildren<Renderer>(true);
            foreach (var target in _renderers)
            {
                if (!material)
                    continue;
                var materials = new Material[target.sharedMaterials.Length];
                for (var i = 0; i < materials.Length; i++) materials[i] = material;
                target.sharedMaterials = materials;
            }
        }

        // Places the ghost on the grid and tints it (valid green / invalid red).
        public void Show(FacilityData data, Vector2Int origin, int rotation, bool valid)
        {
            if (!_root)
                return;

            var pivot = CellToWorld(FacilityFootprint.PivotCell(origin, data.Size, rotation));
            _model.transform.SetPositionAndRotation(pivot, FacilityFootprint.RotationOf(rotation));

            var size = FacilityFootprint.RotatedSize(data.Size, rotation);
            var corner = CellToWorld(origin);
            _footprint.transform.position = new Vector3(corner.x + size.x * 0.5f, corner.y + 0.02f, corner.z + size.y * 0.5f);
            _footprint.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _footprint.transform.localScale = new Vector3(size.x, size.y, 1f);

            var settings = GameConfig.Instance.BuildMode;
            Tint(valid ? settings.ValidColor : settings.InvalidColor);
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root)
                _root.SetActive(false);
        }

        public void Clear()
        {
            if (_root)
                Object.Destroy(_root);
            _root = null;
            _model = null;
            _footprint = null;
            _renderers = null;
        }

        // NOTE: One shared material; the color goes through a property block (no material copies).
        private void Tint(Color color)
        {
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            foreach (var target in _renderers)
            {
                if (target)
                    target.SetPropertyBlock(_block);
            }
        }

        // NOTE: DestroyImmediate is safe here — these are runtime clones, never assets.
        private static void Strip(GameObject model)
        {
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(behaviour);
        }

        private static GameObject CreateBox(Vector2Int size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject pivot = new("Placeholder");
            box.transform.SetParent(pivot.transform, false);
            box.transform.localPosition = new Vector3(size.x * 0.5f, 0.5f, size.y * 0.5f);
            box.transform.localScale = new Vector3(size.x, 1f, size.y);
            return pivot;
        }

        private Vector3 CellToWorld(Vector2Int cell) => _grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
    }
}