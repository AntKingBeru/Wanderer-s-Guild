// Pure resolver: computes the origin at which a footprint's door connects to a clicked door.

public static class BuildPlacementResolver
{
    public static bool TryResolve(DoorKey clicked, RoomFootprint footprint, out TileCoord origin)
    {
        origin = default;
        if (!footprint || !GuildGrid.Exists)
            return false;

        var opposite = DoorGeometry.Opposite(clicked.edge);
        var target = clicked.tile + DoorGeometry.OffsetFor(clicked.edge);

        foreach (var door in footprint.Doors)
        {
            if (door.edge != opposite) continue;
            // origin so this door lands on the target tile: origin + d.Tile = target.
            var candidateOrigin = new TileCoord(target.x - door.tile.x, target.z - door.tile.z);
            if (GuildGrid.Instance.CanPlace(footprint, candidateOrigin) &&
                ConnectionValidator.HasConnection(footprint, candidateOrigin))
            {
                origin = candidateOrigin;
                return true;
            }
        }
        return false;
    }
}