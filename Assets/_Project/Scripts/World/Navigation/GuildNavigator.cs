// The avatars' navigation facade: the room grid, furniture blocks and wall clearance, path finding and smoothing — one API.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Facade — AvatarManager and processions ask "is it free / where's a free cell / how do I get there"; the grid,
    // clearance map, A* and smoother stay behind it.
    public sealed class GuildNavigator
    {
        private readonly Grid _unityGrid;
        private readonly GuildNavGrid _grid = new();
        private readonly NavClearanceMap _clearance = new();
        private readonly GridPathfinder _pathfinder = new();
        private readonly PathSmoother _smoother = new();
        private readonly List<Vector2Int> _free = new();
        private readonly List<Vector2Int> _neighbours = new();
        private readonly List<Vector2Int> _layer = new();
        private readonly List<Vector2Int> _nextLayer = new();
        private readonly HashSet<Vector2Int> _visited = new();
        private int _obstacleVersion = -1;

        public GuildNavigator(Grid unityGrid) => _unityGrid = unityGrid;

        public GuildNavGrid Grid => _grid;
        public NavClearanceMap Clearance => _clearance;
        public int CellCount => _grid.CellCount;

        private static NavigationSettings Settings => GameConfig.Instance.Navigation;

        // Rooms changed: rebuild the grid and the furniture/clearance on top of it.
        public void Rebuild(IReadOnlyList<PlacedFacility> rooms)
        {
            _grid.Rebuild(rooms);
            RebuildObstacles();
        }

        // NOTE: Call every frame — furniture appears/disappears a frame after its room's event (deferred Destroy, prefab
        // swaps), so blocks follow the obstacle registry's version. True if it rebuilt.
        public bool RefreshObstacles()
        {
            if (_obstacleVersion == NavObstacleRegistry.Version)
                return false;
            RebuildObstacles();
            return true;
        }

        public bool IsWalkable(Vector2Int cell) => _grid.IsWalkable(cell);
        public bool IsFree(Vector2Int cell) => _grid.IsWalkable(cell) && !_clearance.IsBlocked(cell);
        public int RoomOf(Vector2Int cell) => _grid.RoomOf(cell);

        public bool TryRandomFreeCell(out Vector2Int cell)
        {
            cell = default;
            if (_free.Count == 0)
                return false;
            cell = _free[Random.Range(0, _free.Count)];
            return true;
        }

        // NOTE: `from` itself if free; otherwise ring by ring (real steps, inside from's room — blocked cells are crossed but
        // never returned), and of the nearest free cells the one closest to preferNear wins (approach from your own side).
        // A cell off the grid is first snapped to the nearest walkable one.
        public bool TryNearestFree(Vector2Int from, Vector2Int preferNear, out Vector2Int cell)
        {
            cell = from;
            if (!_grid.TryNearest(from, out var start))
                return false;
            if (IsFree(start))
            {
                cell = start;
                return true;
            }

            var room = _grid.RoomOf(start);
            _visited.Clear();
            _layer.Clear();
            _layer.Add(start);
            _visited.Add(start);

            while (_layer.Count > 0)
            {
                _nextLayer.Clear();
                foreach (var current in _layer)
                {
                    _grid.Neighbours(current, _neighbours);
                    foreach (var next in _neighbours)
                    {
                        if (_grid.RoomOf(next) == room && _visited.Add(next))
                            _nextLayer.Add(next);
                    }
                }

                var found = false;
                var best = int.MaxValue;
                foreach (var candidate in _nextLayer)
                {
                    if (!IsFree(candidate))
                        continue;
                    var distance = (candidate - preferNear).sqrMagnitude;
                    if (distance >= best)
                        continue;
                    best = distance;
                    cell = candidate;
                    found = true;
                }
                if (found)
                    return true;

                _layer.Clear();
                _layer.AddRange(_nextLayer);
            }
            return false;
        }

        // Path from start (can be blocked — getting up) to a free goal; straightened if enabled.
        public bool TryFindPath(Vector2Int start, Vector2Int goal, int maxNodes, List<Vector2Int> path)
        {
            if (!_pathfinder.TryFind(_grid, _clearance, start, goal, maxNodes, path))
                return false;
            if (Settings.SmoothPaths)
                _smoother.Smooth(_grid, _clearance, start, path);
            return true;
        }

        private void RebuildObstacles()
        {
            _obstacleVersion = NavObstacleRegistry.Version;
            var settings = Settings;
            _clearance.Rebuild(_grid, _unityGrid, NavObstacleRegistry.All, settings.ObstaclePadding, settings.ClearanceCosts);

            _free.Clear();
            foreach (var cell in _grid.Cells)
            {
                if (!_clearance.IsBlocked(cell))
                    _free.Add(cell);
            }
        }
    }
}