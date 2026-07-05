// Singleton grid: tracks tile occupancy, converts world<->tile, and validates room placements.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-86)]
public class GuildGrid : MonoSingleton<GuildGrid>
{
    private readonly Dictionary<TileCoord, FacilityType> _occupied = new Dictionary<TileCoord, FacilityType>();
    
    public Vector3 TileToWorld(TileCoord tile)
    {
        var config = GameConfig.Instance.Grid;
        return config.gridOrigin + new Vector3((tile.x + 0.5f) * config.tileSize, 0f, (tile.z + 0.5f) * config.tileSize);
    }
    
    public TileCoord WorldToTile(Vector3 world)
    {
        var config = GameConfig.Instance.Grid;
        var local = world - config.gridOrigin;
        return new TileCoord(Mathf.FloorToInt(local.x / config.tileSize), Mathf.FloorToInt(local.z / config.tileSize));
    }

    public bool InBounds(TileCoord tile)
    {
        var config = GameConfig.Instance.Grid;
        return tile is { x: >= 0, z: >= 0 } && tile.x < config.width && tile.z < config.depth;
    }

    public bool IsFree(TileCoord tile)
        => InBounds(tile) && !_occupied.ContainsKey(tile);
    
    public bool CanPlace(RoomFootprint footprint, TileCoord origin)
        => footprint && footprint.TilesAt(origin).All(IsFree);
    
    public void Occupy(RoomFootprint footprint, TileCoord origin, FacilityType type)
    {
        foreach (var tile in footprint.TilesAt(origin))
            _occupied[tile] = type;
    }

    public bool TryGetOccupant(TileCoord tile, out FacilityType type)
        => _occupied.TryGetValue(tile, out type);
}