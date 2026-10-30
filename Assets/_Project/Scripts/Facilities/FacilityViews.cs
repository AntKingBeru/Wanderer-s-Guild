// Spawns and refreshes the visible side of rooms: the level's prefab at the grid pivot, plus a scaffold while building.
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
        // footprint during any construction or upgrade.
        public void Refresh(PlacedFacility room)
        {
            if (!_views.TryGetValue(room.Id, out var view))
                _views[room.Id] = view = new View();

            if (view.ModelLevel != room.Level)
            {
                if (view.Model)
                    Object.Destroy(view.Model);
                view.Model = room.Level > 0 ? SpawnModel(room) : null;
                view.ModelLevel = room.Level;
            }

            var needsScaffold = room.IsUnderConstruction;
            switch (needsScaffold)
            {
                case true when !view.Scaffold:
                    view.Scaffold = SpawnScaffold(room);
                    break;
                case false when view.Scaffold:
                    Object.Destroy(view.Scaffold);
                    view.Scaffold = null;
                    break;
            }
        }

        public void Remove(PlacedFacility room)
        {
            if (!_views.TryGetValue(room.Id, out var view))
                return;

            if (view.Model)
                Object.Destroy(view.Model);
            if (view.Scaffold)
                Object.Destroy(view.Scaffold);
            _views.Remove(room.Id);
        }

        // World position of a cell's min corner (Grid with Cell Swizzle XZY: grid y = world z).
        public Vector3 CellToWorld(Vector2Int cell) => _grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));

        private GameObject SpawnModel(PlacedFacility room)
        {
            var prefab = room.Data.PrefabFor(room.Level);
            if (!prefab)
            {
                Debug.LogWarning($"[Facilities] '{room.Data.Id}' has no prefab for level {room.Level}; showing a box.");
                return SpawnBox(room, 1f, $"{room.Data.DisplayName} (placeholder)");
            }

            var pivot = CellToWorld(FacilityFootprint.PivotCell(room.Origin, room.Data.Size, room.Rotation));
            var model = Object.Instantiate(prefab, pivot, FacilityFootprint.RotationOf(room.Rotation), _parent);
            model.name = $"{room.Data.DisplayName} #{room.Id}";
            return model;
        }

        private GameObject SpawnScaffold(PlacedFacility room)
        {
            if (!_settings.ScaffoldPrefab)
                return SpawnBox(room, 0.5f, $"Scaffold #{room.Id}");

            var size = FacilityFootprint.RotatedSize(room.Data.Size, room.Rotation);
            var scaffold = Object.Instantiate(_settings.ScaffoldPrefab, FootprintCentre(room, 0f), Quaternion.identity, _parent);
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