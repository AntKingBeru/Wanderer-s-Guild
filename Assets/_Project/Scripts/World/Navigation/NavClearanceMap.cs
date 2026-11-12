// Per-cell extras on top of the room grid: cells blocked by furniture, and how near each free cell is to a wall, another room
// or furniture (paths pay extra there, so avatars keep to the middle of rooms).
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class NavClearanceMap
    {
        private static readonly Vector2Int[] Around =
        {
            new(-1, -1), new(0, -1), new(1, -1), new(-1, 0), new(1, 0), new(-1, 1), new(0, 1), new(1, 1)
        };

        private readonly HashSet<Vector2Int> _blocked = new();
        private readonly Dictionary<Vector2Int, int> _distance = new();
        private readonly Queue<Vector2Int> _frontier = new();
        private readonly List<Vector2Int> _neighbours = new();
        private float[] _costs = System.Array.Empty<float>();

        public int BlockedCount => _blocked.Count;

        public bool IsBlocked(Vector2Int cell) => _blocked.Contains(cell);

        // 1 = touching a wall / another room / furniture, 2 = one cell further, … ; 0 = beyond the costed rings (or blocked).
        public int DistanceToEdge(Vector2Int cell) => _distance.GetValueOrDefault(cell, 0);

        // Extra path cost for stepping onto this cell (0 in the open).
        public float ExtraCost(Vector2Int cell) =>
            _distance.TryGetValue(cell, out var distance) && distance <= _costs.Length ? _costs[distance - 1] : 0f;

        // NOTE: Costs are clamped to ≥ 0 — a negative cost would make A*'s heuristic overestimate.
        public void Rebuild(GuildNavGrid nav, Grid grid, IReadOnlyList<NavObstacle> obstacles, float padding,
                            IReadOnlyList<float> costs)
        {
            _blocked.Clear();
            _distance.Clear();
            _frontier.Clear();

            _costs = new float[costs.Count];
            for (var i = 0; i < _costs.Length; i++)
                _costs[i] = Mathf.Max(0f, costs[i]);

            if (grid != null)
                MarkBlocked(nav, grid, obstacles, padding);
            if (_costs.Length > 0)
                MarkRings(nav);
        }

        // NOTE: Only walkable cells can be blocked; each obstacle tests the cells under its bounds by their centers.
        private void MarkBlocked(GuildNavGrid nav, Grid grid, IReadOnlyList<NavObstacle> obstacles, float padding)
        {
            foreach (var obstacle in obstacles)
            {
                if (obstacle == null || !obstacle.isActiveAndEnabled)
                    continue;

                var rect = obstacle.FloorRect(padding);
                var min = grid.WorldToCell(new Vector3(rect.xMin, 0f, rect.yMin));
                var max = grid.WorldToCell(new Vector3(rect.xMax, 0f, rect.yMax));
                var y = obstacle.transform.position.y;

                for (var x = min.x; x <= max.x; x++)
                for (var z = min.y; z <= max.y; z++)
                {
                    var cell = new Vector2Int(x, z);
                    if (!nav.IsWalkable(cell) || _blocked.Contains(cell))
                        continue;

                    var corner = grid.CellToWorld(new Vector3Int(x, z, 0));
                    if (obstacle.Covers(new Vector3(corner.x + 0.5f, y, corner.z + 0.5f), padding))
                        _blocked.Add(cell);
                }
            }
        }

        // NOTE: Multi-source BFS: free cells touching an edge are ring 1, spreading inward (same room, free cells) up to the
        // number of costed rings.
        private void MarkRings(GuildNavGrid nav)
        {
            foreach (var cell in nav.Cells)
            {
                if (_blocked.Contains(cell) || !TouchesEdge(nav, cell))
                    continue;
                _distance[cell] = 1;
                _frontier.Enqueue(cell);
            }

            while (_frontier.Count > 0)
            {
                var cell = _frontier.Dequeue();
                var distance = _distance[cell];
                if (distance >= _costs.Length)
                    continue;

                var room = nav.RoomOf(cell);
                nav.Neighbours(cell, _neighbours);
                foreach (var next in _neighbours)
                {
                    if (nav.RoomOf(next) != room || _blocked.Contains(next) || _distance.ContainsKey(next))
                        continue;
                    _distance[next] = distance + 1;
                    _frontier.Enqueue(next);
                }
            }
        }

        // NOTE: Any of the 8 surrounding cells off the grid, in another room (a wall between rooms) or blocked.
        private bool TouchesEdge(GuildNavGrid nav, Vector2Int cell)
        {
            var room = nav.RoomOf(cell);
            foreach (var offset in Around)
            {
                var next = cell + offset;
                if (nav.RoomOf(next) != room || _blocked.Contains(next))
                    return true;
            }
            return false;
        }
    }
}