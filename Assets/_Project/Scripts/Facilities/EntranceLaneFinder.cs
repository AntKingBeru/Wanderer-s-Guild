// Finds the strip in front of the Guild Hall's front door that must stay clear, from the GuildEntrance marker and the
// hall's footprint.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: The door side = the hall edge nearest the entrance marker (it stands just inside the front door); the strip is
    // centred on the marker across that edge. No marker, hall or width → no strip (nothing blocked).
    public static class EntranceLaneFinder
    {
        public static EntranceLane Find(Grid grid, PlacedFacility hall, float width)
        {
            var entrance = GuildEntrance.Current;
            if (grid == null || hall == null || entrance == null || width <= 0f)
                return default;

            // NOTE: Cell space (Cell Swizzle XZY applied — point.y is world z); continuous, so the door's center line survives.
            var point = grid.LocalToCellInterpolated(grid.WorldToLocal(entrance.transform.position));
            var size = FacilityFootprint.RotatedSize(hall.Data.Size, hall.Rotation);
            int x0 = hall.Origin.x, y0 = hall.Origin.y, x1 = x0 + size.x, y1 = y0 + size.y;
            var half = width * 0.5f;

            var west = point.x - x0;
            var east = x1 - point.x;
            var south = point.y - y0;
            var north = y1 - point.y;
            var nearest = Mathf.Min(Mathf.Min(west, east), Mathf.Min(south, north));

            if (Mathf.Approximately(nearest, west))
                return new EntranceLane(Vector2Int.left, x0 - 1, point.y, half);
            if (Mathf.Approximately(nearest, east))
                return new EntranceLane(Vector2Int.right, x1, point.y, half);
            return Mathf.Approximately(nearest, south)
                ? new EntranceLane(Vector2Int.down, y0 - 1, point.x, half)
                : new EntranceLane(Vector2Int.up, y1, point.x, half);
        }
    }
}