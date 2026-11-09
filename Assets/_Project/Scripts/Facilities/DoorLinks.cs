// Door-link rules for visuals (pure): which finished doors exist, whether a door is met by another finished room, and which of
// a room's doors a connector marker belongs to.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class DoorLinks
    {
        // Every door of every finished room (level ≥ 1) — the same rooms the adventurers' paths use.
        public static void CollectFinished(IReadOnlyList<PlacedFacility> rooms, HashSet<WorldDoor> results)
        {
            results.Clear();
            foreach (var room in rooms)
            {
                if (room.Level <= 0)
                    continue;
                foreach (var door in room.Doors)
                    results.Add(door);
            }
        }

        // True if a finished room has the door that meets this one (both outside cells, facing back).
        public static bool IsLinked(WorldDoor door, HashSet<WorldDoor> finishedDoors) => finishedDoors.Contains(door.Partner);

        // The room's door whose world center is nearest to a point (the connector marker sits on its doorway's
        // center). False if the room has no doors.
        public static bool TryNearest(PlacedFacility room, Vector3 worldPoint, Grid grid, out WorldDoor nearest)
        {
            nearest = default;
            var best = float.MaxValue;
            var found = false;
            foreach (var door in room.Doors)
            {
                var centre = (CellCentre(grid, door.A) + CellCentre(grid, door.B)) * 0.5f;
                var distance = (new Vector2(centre.x, centre.z) - new Vector2(worldPoint.x, worldPoint.z)).sqrMagnitude;
                if (distance >= best)
                    continue;

                best = distance;
                nearest = door;
                found = true;
            }
            return found;
        }

        private static Vector3 CellCentre(Grid grid, Vector2Int cell) =>
            grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0)) + new Vector3(0.5f, 0f, 0.5f);
    }
}