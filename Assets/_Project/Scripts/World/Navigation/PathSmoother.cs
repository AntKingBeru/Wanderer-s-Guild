// Straightens a cell path: skips waypoints while the straight line from the last kept point stays on free cells, steps only
// where the grid allows, and comes no nearer to walls or furniture than its own ends.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class PathSmoother
    {
        private const float SampleStep = 0.25f; // NOTE: Cells; small enough that a line never skips a cell.

        private readonly List<Vector2Int> _kept = new();

        // NOTE: Greedy — from each kept point, the farthest point reachable in a straight line is kept next. The last cell is
        // always kept. path excludes the start (the avatar's cell, which may be blocked).
        public void Smooth(GuildNavGrid grid, NavClearanceMap clearance, Vector2Int start, List<Vector2Int> path)
        {
            if (path.Count < 2)
                return;

            _kept.Clear();
            var anchor = start;
            var i = 0;
            while (i < path.Count)
            {
                var far = i;
                for (var j = i + 1; j < path.Count && HasClearLine(grid, clearance, anchor, path[j]); j++)
                    far = j;

                _kept.Add(path[far]);
                anchor = path[far];
                i = far + 1;
            }

            path.Clear();
            path.AddRange(_kept);
        }

        // NOTE: Samples the line between the two cell centres; every change of cell must be a legal step, and cells in
        // between may not be nearer an edge than the nearer of the two ends (a line between open cells stays in the open).
        private static bool HasClearLine(GuildNavGrid grid, NavClearanceMap clearance, Vector2Int a, Vector2Int b)
        {
            var from = new Vector2(a.x + 0.5f, a.y + 0.5f);
            var to = new Vector2(b.x + 0.5f, b.y + 0.5f);
            var samples = Mathf.CeilToInt(Vector2.Distance(from, to) / SampleStep);
            var limit = Mathf.Min(Closeness(clearance, a), Closeness(clearance, b));

            var previous = a;
            for (var s = 1; s <= samples; s++)
            {
                var point = Vector2.Lerp(from, to, (float)s / samples);
                var cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
                if (cell == previous)
                    continue;

                if (!CanMove(grid, clearance, previous, cell))
                    return false;
                if (cell != b && Closeness(clearance, cell) < limit)
                    return false;
                previous = cell;
            }
            return previous == b;
        }

        // NOTE: Same rules as the pathfinder: orthogonal = a grid step into a free cell; diagonal = inside one room with both
        // side cells free.
        private static bool CanMove(GuildNavGrid grid, NavClearanceMap clearance, Vector2Int from, Vector2Int to)
        {
            var dx = to.x - from.x;
            var dy = to.y - from.y;
            if (clearance.IsBlocked(to))
                return false;

            if (Mathf.Abs(dx) + Mathf.Abs(dy) == 1)
                return grid.CanStep(from, to);

            if (Mathf.Abs(dx) != 1 || Mathf.Abs(dy) != 1)
                return false;

            var room = grid.RoomOf(from);
            var sideA = new Vector2Int(to.x, from.y);
            var sideB = new Vector2Int(from.x, to.y);
            return grid.RoomOf(to) == room &&
                   grid.RoomOf(sideA) == room && !clearance.IsBlocked(sideA) &&
                   grid.RoomOf(sideB) == room && !clearance.IsBlocked(sideB);
        }

        // NOTE: Ring number near an edge (1 = touching); cells in the open count as "infinitely" clear.
        private static int Closeness(NavClearanceMap clearance, Vector2Int cell)
        {
            var distance = clearance.DistanceToEdge(cell);
            return distance == 0 ? int.MaxValue : distance;
        }
    }
}