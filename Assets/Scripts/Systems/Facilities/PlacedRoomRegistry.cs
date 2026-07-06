// Singleton registry of placed rooms (footprint + origin) for connectivity queries.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-86)]
public class PlacedRoomRegistry : MonoSingleton<PlacedRoomRegistry>
{
    private readonly List<PlacedRoom> _rooms = new List<PlacedRoom>();
    private readonly HashSet<DoorKey> _usedDoors = new HashSet<DoorKey>();
    
    public IReadOnlyList<PlacedRoom> Rooms => _rooms;

    public void Register(FacilityType type, RoomFootprint footprint, TileCoord origin)
        => _rooms.Add(new PlacedRoom(type, footprint, origin));
    
    public bool IsDoorUsed(DoorKey key)
        => _usedDoors.Contains(key);
    
    public void MarkDoorUsed(DoorKey key)
        => _usedDoors.Add(key);
}