// A* over the guild's nav grid (4 directions, Manhattan heuristic, node-capped). Returns the cells after the start.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class GridPathfinder
    {
        private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new();
        private readonly Dictionary<Vector2Int, int> _cost = new();
        private readonly List<(Vector2Int cell, int priority)> _open = new();
        private readonly HashSet<Vector2Int> _closed = new();
        private readonly List<Vector2Int> _neighbours = new();

        // Fills <paramref name="path"/> with the cells to walk (start excluded, goal included). False if unreachable
        // or the node cap was hit.
        // NOTE: The open set is a plain list with a linear min search — guild grids are a few hundred cells, so a heap
        // wouldn't pay for itself.
        public bool TryFind(GuildNavGrid grid, Vector2Int start, Vector2Int goal, int maxNodes, List<Vector2Int> path)
        {
            path.Clear();
            if (!grid.IsWalkable(start) || !grid.IsWalkable(goal))
                return false;
            if (start == goal)
                return true;

            _cameFrom.Clear();
            _cost.Clear();
            _open.Clear();
            _closed.Clear();

            _cost[start] = 0;
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

                grid.Neighbours(current, _neighbours);
                foreach (var next in _neighbours)
                {
                    if (_closed.Contains(next))
                        continue;

                    var cost = _cost[current] + 1;
                    if (_cost.TryGetValue(next, out var known) && cost >= known)
                        continue;

                    _cost[next] = cost;
                    _cameFrom[next] = current;
                    _open.Add((next, cost + Heuristic(next, goal)));
                }
            }

            return false;
        }

        private void Rebuild(Vector2Int start, Vector2Int goal, List<Vector2Int> path)
        {
            for (var cell = goal; cell != start; cell = _cameFrom[cell])
                path.Add(cell);
            path.Reverse();
        }

        private static int Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}