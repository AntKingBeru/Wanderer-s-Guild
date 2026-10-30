// Occupancy + connector map of the guild's rooms on the grid; answers overlap, connection and reachability questions.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class FacilityGrid
    {
        private readonly Dictionary<Vector2Int, int> _occupied = new();
        private readonly Dictionary<(Vector2Int, CardinalDirection), int> _connectors = new();
        private readonly Dictionary<int, PlacedFacility> _rooms = new();

        public void Add(PlacedFacility room)
        {
            _rooms[room.Id] = room;
            foreach (var cell in room.Cells)
                _occupied[cell] = room.Id;
            foreach (var (cell, facing) in room.Connectors)
                _connectors[(cell, facing)] = room.Id;
        }

        public void Remove(PlacedFacility room)
        {
            _rooms.Remove(room.Id);
            foreach (var cell in room.Cells)
                _occupied.Remove(cell);
            foreach (var (cell, facing) in room.Connectors)
                _connectors.Remove((cell, facing));
        }

        public bool IsFree(IEnumerable<Vector2Int> cells)
        {
            return cells.All(cell => !_occupied.ContainsKey(cell));
        }

        // Room ids linked to these connectors: the neighboring cell has a connector facing back.
        public void LinkedRooms(IEnumerable<(Vector2Int cell, CardinalDirection facing)> connectors, int ignoreId, List<int> results)
        {
            results.Clear();
            foreach (var (cell, facing) in connectors)
            {
                var neighbour = cell + FacilityFootprint.Step(facing);
                if (_connectors.TryGetValue((neighbour, FacilityFootprint.Opposite(facing)), out var id) &&
                    id != ignoreId && !results.Contains(id))
                    results.Add(id);
            }
        }

        // True if every room except <paramref name="removedId"/> stays reachable from the root room.
        // NOTE: Breadth-first search over connector links, skipping the room being removed.
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
                LinkedRooms(room.Connectors, room.Id, linked);
                foreach (var id in linked.Where(id => id != removedId && visited.Add(id)))
                {
                    queue.Enqueue(id);
                }
            }

            return visited.Count == _rooms.Count - (_rooms.ContainsKey(removedId) ? 1 : 0);
        }
    }
}