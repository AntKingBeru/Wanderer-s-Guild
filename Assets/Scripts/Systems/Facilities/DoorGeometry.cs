// Pure helper: resolves door edge directions and tests whether two placed doors connect.

public static class DoorGeometry
{
    public static TileCoord OffsetFor(TileEdge edge) => edge switch
    {
        TileEdge.North => new TileCoord(0, 1),
        TileEdge.South => new TileCoord(0, -1),
        TileEdge.East  => new TileCoord(1, 0),
        TileEdge.West  => new TileCoord(-1, 0),
        _ => new TileCoord(0, 0)
    };
    
    public static TileEdge Opposite(TileEdge edge) => edge switch
    {
        TileEdge.North => TileEdge.South,
        TileEdge.South => TileEdge.North,
        TileEdge.East  => TileEdge.West,
        TileEdge.West  => TileEdge.East,
        _ => edge
    };
    
    public static TileCoord DoorTile(DoorPoint door, TileCoord roomOrigin)
        => roomOrigin + door.tile;
    
    public static TileCoord TargetTile(DoorPoint door, TileCoord roomOrigin)
        => roomOrigin + door.tile + OffsetFor(door.edge);
    
    public static bool Connects(DoorPoint a, TileCoord aOrigin, DoorPoint b, TileCoord bOrigin)
    {
        if (a.edge != Opposite(b.edge))
            return false;
        return TargetTile(a, aOrigin) == DoorTile(b, bOrigin)
               && TargetTile(b, bOrigin) == DoorTile(a, aOrigin);
    }
}