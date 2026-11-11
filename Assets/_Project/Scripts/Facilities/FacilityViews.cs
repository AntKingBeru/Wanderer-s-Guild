// Spawns and refreshes the visible side of rooms (level prefab at the grid pivot, scaffold while building) and outlines rooms.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class FacilityViews
    {
        private sealed class View
        {
            public GameObject Model;
            public GameObject Scaffold;
            public int ModelLevel = -1;
            public bool Highlighted;
            public readonly Dictionary<Renderer, uint> OriginalLayers = new();
        }

        private readonly Grid _grid;
        private readonly Transform _parent;
        private readonly FacilitySettings _settings;
        private readonly Dictionary<int, View> _views = new();

        public FacilityViews(Grid grid, Transform parent, FacilitySettings settings)
        {
            _grid = grid;
            _parent = parent;
            _settings = settings;
        }

        // NOTE: The model shows the finished level (none while a new room is still being built); the scaffold covers the
        // footprint during any construction or upgrade. An outlined room is un-outlined before any swap and re-outlined
        // after, so new renderers get the outline and nothing keeps a stale bit.
        public void Refresh(PlacedFacility room)
        {
            if (!_views.TryGetValue(room.Id, out var view))
                _views[room.Id] = view = new View();

            var needsScaffold = room.IsUnderConstruction;
            var changes = view.ModelLevel != room.Level || needsScaffold != (view.Scaffold);
            if (!changes)
                return;

            if (view.Highlighted)
                ApplyHighlight(view, false);

            if (view.ModelLevel != room.Level)
            {
                if (view.Model is not null)
                    Object.Destroy(view.Model);
                view.Model = room.Level > 0 ? SpawnModel(room) : null;
                view.ModelLevel = room.Level;
            }

            switch (needsScaffold)
            {
                case true when view.Scaffold is null:
                    view.Scaffold = SpawnScaffold(room);
                    break;
                case false when view.Scaffold is not null:
                    Object.Destroy(view.Scaffold);
                    view.Scaffold = null;
                    break;
            }

            if (view.Highlighted)
                ApplyHighlight(view, true);
        }

        public void Remove(PlacedFacility room)
        {
            if (!_views.TryGetValue(room.Id, out var view))
                return;

            if (view.Highlighted)
                PixelOutlineRequests.Remove();
            if (view.Model is not null)
                Object.Destroy(view.Model);
            if (view.Scaffold is not null)
                Object.Destroy(view.Scaffold);
            _views.Remove(room.Id);
        }

        // Outlines a room (its model and scaffold) with the hover outline, or removes it.
        public void SetHighlighted(PlacedFacility room, bool highlighted)
        {
            if (!_views.TryGetValue(room.Id, out var view) || view.Highlighted == highlighted)
                return;

            view.Highlighted = highlighted;
            ApplyHighlight(view, highlighted);
            if (highlighted)
                PixelOutlineRequests.Add();
            else
                PixelOutlineRequests.Remove();
        }

        // World position of a cell's min corner (Grid with Cell Swizzle XZY: grid y = world z).
        public Vector3 CellToWorld(Vector2Int cell) => _grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
        
        // The room's current model (null while a new room is still being built, or if it has no prefab).
        public GameObject ModelOf(int roomId) => _views.TryGetValue(roomId, out var view) ? view.Model : null;

        // NOTE: Originals are stored once per renderer and restored exactly; destroyed renderers are simply forgotten.
        private static void ApplyHighlight(View view, bool on)
        {
            if (!on)
            {
                foreach (var pair in view.OriginalLayers.Where(pair => pair.Key))
                {
                    pair.Key.renderingLayerMask = pair.Value;
                }
                view.OriginalLayers.Clear();
                return;
            }

            var bit = GameConfig.Instance.Interaction.OutlineBit;
            AddBit(view, view.Model, bit);
            AddBit(view, view.Scaffold, bit);
        }

        private static void AddBit(View view, GameObject target, uint bit)
        {
            if (target is null)
                return;

            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (!view.OriginalLayers.ContainsKey(renderer))
                    view.OriginalLayers[renderer] = renderer.renderingLayerMask;
                renderer.renderingLayerMask = view.OriginalLayers[renderer] | bit;
            }
        }

        private GameObject SpawnModel(PlacedFacility room)
        {
            var prefab = room.Data.PrefabFor(room.Level);
            if (prefab is null)
            {
                Debug.LogWarning($"[Facilities] '{room.Data.Id}' has no prefab for level {room.Level}; showing a box.");
                return SpawnBox(room, 1f, $"{room.Data.DisplayName} (placeholder)");
            }

            var pivot = CellToWorld(FacilityFootprint.PivotCell(room.Origin, room.Data.Size, room.Rotation));
            var model = Object.Instantiate(prefab, pivot, FacilityFootprint.RotationOf(room.Rotation), _parent);
            model.name = $"{room.Data.DisplayName} #{room.Id}";
            return model;
        }

        // NOTE: Same convention as rooms — the scaffold prefab is one 1×1 cell with its pivot at the min corner; it's placed
        // at the footprint's min corner and stretched over the rotated footprint, so it covers exactly the room's cells.
        private GameObject SpawnScaffold(PlacedFacility room)
        {
            if (_settings.ScaffoldPrefab is null)
                return SpawnBox(room, 0.5f, $"Scaffold #{room.Id}");

            var size = FacilityFootprint.RotatedSize(room.Data.Size, room.Rotation);
            var scaffold = Object.Instantiate(_settings.ScaffoldPrefab, CellToWorld(room.Origin), Quaternion.identity, _parent);
            scaffold.transform.localScale = new Vector3(size.x, 1f, size.y);
            scaffold.name = $"Scaffold #{room.Id}";
            return scaffold;
        }

        // NOTE: The collider is removed — placeholder boxes must never block hover rays or the camera.
        private GameObject SpawnBox(PlacedFacility room, float height, string name)
        {
            var size = FacilityFootprint.RotatedSize(room.Data.Size, room.Rotation);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(box.GetComponent<Collider>());
            box.name = name;
            box.transform.SetParent(_parent, false);
            box.transform.position = FootprintCentre(room, height * 0.5f);
            box.transform.localScale = new Vector3(size.x, height, size.y);
            return box;
        }

        private Vector3 FootprintCentre(PlacedFacility room, float y)
        {
            var size = FacilityFootprint.RotatedSize(room.Data.Size, room.Rotation);
            var corner = CellToWorld(room.Origin);
            return new Vector3(corner.x + size.x * 0.5f, corner.y + y, corner.z + size.y * 0.5f);
        }
    }
}