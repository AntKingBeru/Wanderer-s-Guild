// Singleton registry of placed rooms (footprint + origin) for connectivity queries.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-86)]
public class PlacedRoomRegistry : MonoSingleton<PlacedRoomRegistry>
{
    private readonly List<PlacedRoom> _rooms = new List<PlacedRoom>();
    public IReadOnlyList<PlacedRoom> Rooms => _rooms;

    public void Register(FacilityType type, RoomFootprint footprint, TileCoord origin)
        => _rooms.Add(new PlacedRoom(type, footprint, origin));
}