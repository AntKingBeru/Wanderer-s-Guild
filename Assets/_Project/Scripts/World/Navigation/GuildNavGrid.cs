// The walkable grid: every cell of every finished room; moving between rooms only across linked 2-wide doors.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class GuildNavGrid
    {
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        private readonly Dictionary<Vector2Int, int> _roomOf = new();
        private readonly HashSet<(Vector2Int, Vector2Int)> _doorLinks = new();
        private readonly List<Vector2Int> _cells = new();

        public int CellCount => _cells.Count;

        // Rebuilds from the rooms: walkable = finished level (an upgrade keeps it open).
        // NOTE: A crossing needs BOTH rooms' doors to meet (one's partner is the other's door); a door into a wall links nothing.
        public void Rebuild(IReadOnlyList<PlacedFacility> rooms)
        {
            _roomOf.Clear();
            _doorLinks.Clear();
            _cells.Clear();

            HashSet<WorldDoor> doors = new();
            foreach (var room in rooms)
            {
                if (room.Level <= 0)
                    continue;

                foreach (var cell in room.Cells)
                {
                    if (_roomOf.ContainsKey(cell))
                        continue;
                    _roomOf[cell] = room.Id;
                    _cells.Add(cell);
                }
                foreach (var door in room.Doors) doors.Add(door);
            }

            foreach (var door in doors)
            {
                var partner = door.Partner;
                if (!doors.Contains(partner))
                    continue;
                if (!IsWalkable(door.A) || !IsWalkable(door.B) || !IsWalkable(partner.A) || !IsWalkable(partner.B))
                    continue;

                Link(door.A, partner.A);
                Link(door.B, partner.B);
            }
        }

        public bool IsWalkable(Vector2Int cell) => _roomOf.ContainsKey(cell);
        
        // Every walkable cell (rebuilt with the rooms).
        public IReadOnlyList<Vector2Int> Cells => _cells;

        // NOTE: For orthogonally adjacent cells: same room, or a linked door crossing (the same rule as Neighbors).
        public bool CanStep(Vector2Int from, Vector2Int to) =>
            _roomOf.TryGetValue(from, out var a) && _roomOf.TryGetValue(to, out var b) &&
            (a == b || _doorLinks.Contains((from, to)));

        // The room a walkable cell belongs to (−1 if none).
        public int RoomOf(Vector2Int cell) => _roomOf.GetValueOrDefault(cell, -1);

        // Reachable neighbors: same-room steps, or a linked door crossing.
        public void Neighbours(Vector2Int cell, List<Vector2Int> results)
        {
            results.Clear();
            if (!_roomOf.TryGetValue(cell, out int room)) return;

            foreach (var direction in Directions)
            {
                var next = cell + direction;
                if (!_roomOf.TryGetValue(next, out var nextRoom))
                    continue;
                if (nextRoom == room || _doorLinks.Contains((cell, next))) results.Add(next);
            }
        }

        public bool TryRandomCell(out Vector2Int cell)
        {
            cell = default;
            if (_cells.Count == 0)
                return false;

            cell = _cells[Random.Range(0, _cells.Count)];
            return true;
        }

        // The walkable cell nearest to this one (itself if walkable). False if the grid is empty.
        public bool TryNearest(Vector2Int from, out Vector2Int nearest)
        {
            nearest = from;
            if (IsWalkable(from))
                return true;
            if (_cells.Count == 0)
                return false;

            var best = int.MaxValue;
            foreach (var cell in _cells)
            {
                var distance = Mathf.Abs(cell.x - from.x) + Mathf.Abs(cell.y - from.y);
                if (distance >= best)
                    continue;
                best = distance;
                nearest = cell;
            }
            return true;
        }

        private void Link(Vector2Int a, Vector2Int b)
        {
            _doorLinks.Add((a, b));
            _doorLinks.Add((b, a));
        }
    }
}