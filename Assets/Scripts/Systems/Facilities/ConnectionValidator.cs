// Validates room placement connectivity: collects door connections to existing rooms, skipping used doors.

using System.Linq;
using System.Collections.Generic;

public static class ConnectionValidator
{
    public static List<(DoorKey candidate, DoorKey existing)> CollectConnections(
        RoomFootprint candidate, TileCoord candidateOrigin)
    {
        var result = new List<(DoorKey, DoorKey)>();
        if (!candidate || !PlacedRoomRegistry.Exists)
            return result;
        var reg = PlacedRoomRegistry.Instance;

        foreach (var placed in reg.Rooms)
        {
            if (!placed.footprint)
                continue;
            foreach (var cd in candidate.Doors) result.AddRange(from pd in placed.footprint.Doors let pdKey = DoorGeometry.KeyOf(pd, placed.origin)
                where !reg.IsDoorUsed(pdKey) where DoorGeometry.Connects(cd, candidateOrigin, pd, placed.origin) select (DoorGeometry.KeyOf(cd, candidateOrigin), pdKey));
        }
        return result;
    }

    public static bool HasConnection(RoomFootprint candidate, TileCoord candidateOrigin)
        => CollectConnections(candidate, candidateOrigin).Count > 0;
}