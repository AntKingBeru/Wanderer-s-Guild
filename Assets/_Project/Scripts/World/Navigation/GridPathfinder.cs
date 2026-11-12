// A* over the guild's nav grid: 8 directions (no cutting corners), extra cost near walls and furniture, octile heuristic,
// node-capped. Returns the cells after the start.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class GridPathfinder
    {
        private const float Diagonal = 1.41421356f;

        private static readonly Vector2Int[] Diagonals = { new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) };

        private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new();
        private readonly Dictionary<Vector2Int, float> _cost = new();
        private readonly List<(Vector2Int cell, float priority)> _open = new();
        private readonly HashSet<Vector2Int> _closed = new();
        private readonly List<Vector2Int> _neighbours = new();
        private readonly List<(Vector2Int cell, float length)> _steps = new();

        // Fills <paramref name="path"/> with the cells to walk (start excluded, goal included). False if unreachable, the goal
        // is blocked, or the node cap was hit.
        // NOTE: The start may be blocked (getting up from a chair or bed); blocked cells are never entered otherwise. The open
        // set is a plain list with a linear min search — guild grids are a few hundred cells.
        public bool TryFind(GuildNavGrid grid, NavClearanceMap clearance, Vector2Int start, Vector2Int goal, int maxNodes,
                            List<Vector2Int> path)
        {
            path.Clear();
            if (!grid.IsWalkable(start) || !grid.IsWalkable(goal) || clearance.IsBlocked(goal))
                return false;
            if (start == goal)
                return true;

            _cameFrom.Clear();
            _cost.Clear();
            _open.Clear();
            _closed.Clear();

            _cost[start] = 0f;
            _open.Add((start, Heuristic(start, goal)));

            var visited = 0;
            while (_open.Count > 0)
            {
                var best = 0;
                for (var i = 1; i < _open.Count; i++)
                {
                    if (_open[i].priority < _open[best].priority)
                        best = i;
                }

                var current = _open[best].cell;
                _open.RemoveAt(best);
                if (!_closed.Add(current))
                    continue;

                if (current == goal)
                {
                    Rebuild(start, goal, path);
                    return true;
                }

                if (++visited > maxNodes)
                    return false;

                CollectSteps(grid, clearance, current);
                foreach (var (next, length) in _steps)
                {
                    if (_closed.Contains(next))
                        continue;

                    var cost = _cost[current] + length + clearance.ExtraCost(next);
                    if (_cost.TryGetValue(next, out var known) && cost >= known)
                        continue;

                    _cost[next] = cost;
                    _cameFrom[next] = current;
                    _open.Add((next, cost + Heuristic(next, goal)));
                }
            }

            return false;
        }

        // NOTE: Orthogonal steps follow the grid (same room, or across a linked door); diagonal steps only inside one room and
        // only when both cells beside the corner are free — no cutting corners of walls, door jambs or furniture.
        private void CollectSteps(GuildNavGrid grid, NavClearanceMap clearance, Vector2Int current)
        {
            _steps.Clear();

            grid.Neighbours(current, _neighbours);
            foreach (var next in _neighbours)
            {
                if (!clearance.IsBlocked(next))
                    _steps.Add((next, 1f));
            }

            var room = grid.RoomOf(current);
            foreach (var direction in Diagonals)
            {
                var next = current + direction;
                if (IsFreeIn(grid, clearance, next, room) &&
                    IsFreeIn(grid, clearance, new Vector2Int(next.x, current.y), room) &&
                    IsFreeIn(grid, clearance, new Vector2Int(current.x, next.y), room))
                    _steps.Add((next, Diagonal));
            }
        }

        private static bool IsFreeIn(GuildNavGrid grid, NavClearanceMap clearance, Vector2Int cell, int room) =>
            grid.RoomOf(cell) == room && !clearance.IsBlocked(cell);

        private void Rebuild(Vector2Int start, Vector2Int goal, List<Vector2Int> path)
        {
            for (var cell = goal; cell != start; cell = _cameFrom[cell])
                path.Add(cell);
            path.Reverse();
        }

        // NOTE: Octile distance — exact on an empty 8-way grid, never more than the real cost (extra costs are ≥ 0).
        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            float dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            return dx + dy + (Diagonal - 2f) * Mathf.Min(dx, dy);
        }
    }
}