// Pure-ish validator: checks a candidate room placement connects to an existing room via matching doors.

using System.Linq;

public static class ConnectionValidator
{
    public static bool HasConnection(RoomFootprint candidate, TileCoord candidateOrigin)
    {
        if (!candidate || !PlacedRoomRegistry.Exists)
            return false;

        return PlacedRoomRegistry.Instance.Rooms.Where(placed
            => placed.Footprint).Any(placed => candidate.Doors.Any(cd
            => placed.Footprint.Doors.Any(pd
                => DoorGeometry.Connects(cd, candidateOrigin, pd, placed.Origin))));
    }
}