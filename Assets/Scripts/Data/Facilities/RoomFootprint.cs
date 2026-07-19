// ScriptableObject prototype: a room's occupied tiles (relative to origin) and door connection points.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomFootprint", menuName = "Wanderer's Guild/Room Footprint")]
public class RoomFootprint : ScriptableObject
{
    [Tooltip("Occupied tiles relative to the room's origin tile (0,0 = origin).")]
    [SerializeField] private List<TileCoord> tiles = new List<TileCoord> { new(0, 0) };
    [Tooltip("Door connection points, tiles relative to origin.")]
    [SerializeField] private List<DoorPoint> doors = new List<DoorPoint>();

    public IReadOnlyList<TileCoord> Tiles => tiles;
    public IReadOnlyList<DoorPoint> Doors => doors;
    
    public IEnumerable<TileCoord> TilesAt(TileCoord origin)
    {
        return tiles.Select(tile => origin + tile);
    }
}