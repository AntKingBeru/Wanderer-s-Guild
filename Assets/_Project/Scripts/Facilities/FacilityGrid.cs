// Occupancy + door map of the guild's rooms on the grid; answers overlap, connection and reachability questions.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class FacilityGrid
    {
        private readonly Dictionary<Vector2Int, int> _occupied = new();
        private readonly Dictionary<WorldDoor, int> _doors = new();
        private readonly Dictionary<int, PlacedFacility> _rooms = new();

        public void Add(PlacedFacility room)
        {
            _rooms[room.Id] = room;
            foreach (var cell in room.Cells)
                _occupied[cell] = room.Id;
            foreach (var door in room.Doors)
                _doors[door] = room.Id;
        }

        public void Remove(PlacedFacility room)
        {
            _rooms.Remove(room.Id);
            foreach (var cell in room.Cells)
                _occupied.Remove(cell);
            foreach (var door in room.Doors)
                _doors.Remove(door);
        }

        public bool IsFree(IEnumerable<Vector2Int> cells)
        {
            return cells.All(cell => !_occupied.ContainsKey(cell));
        }

        // True if both cells just outside this door are empty (somewhere a new room could connect).
        public bool IsOpen(WorldDoor door)
        {
            var partner = door.Partner;
            return !_occupied.ContainsKey(partner.A) && !_occupied.ContainsKey(partner.B);
        }

        // Room ids linked to these doors: a door covering both outside cells and facing back.
        public void LinkedRooms(IEnumerable<WorldDoor> doors, int ignoreId, List<int> results)
        {
            results.Clear();
            foreach (var door in doors)
            {
                if (_doors.TryGetValue(door.Partner, out int id) && id != ignoreId && !results.Contains(id))
                    results.Add(id);
            }
        }

        // True if every room except <paramref name="removedId"/> stays reachable from the root room.
        // NOTE: Breadth-first search over door links, skipping the room being removed.
        public bool StaysConnectedWithout(int removedId, int rootId)
        {
            if (removedId == rootId)
                return false;

            HashSet<int> visited = new() { rootId };
            Queue<int> queue = new();
            queue.Enqueue(rootId);
            List<int> linked = new();

            while (queue.Count > 0)
            {
                var room = _rooms[queue.Dequeue()];
                LinkedRooms(room.Doors, room.Id, linked);
                foreach (var id in linked.Where(id => id != removedId && visited.Add(id)))
                {
                    queue.Enqueue(id);
                }
            }

            return visited.Count == _rooms.Count - (_rooms.ContainsKey(removedId) ? 1 : 0);
        }
    }
}