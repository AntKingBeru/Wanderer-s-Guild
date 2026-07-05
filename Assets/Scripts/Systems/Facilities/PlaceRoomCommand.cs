// Command: validates and places a room on the grid, occupying tiles and spawning its instance.

public class PlaceRoomCommand : IInteractionCommand
{
    private readonly FacilityType _type;
    private readonly RoomFootprint _footprint;
    private readonly TileCoord _origin;
    private readonly RoomInstanceFactory _factory;

    public PlaceRoomCommand(FacilityType type, RoomFootprint footprint, TileCoord origin, RoomInstanceFactory factory)
    {
        _type = type;
        _footprint = footprint;
        _origin = origin;
        _factory = factory;
    }
    
    public void Execute()
    {
        if (!GuildGrid.Exists || !GuildGrid.Instance.CanPlace(_footprint, _origin))
            return;
        if (!ConnectionValidator.HasConnection(_footprint, _origin))
            return;

        GuildGrid.Instance.Occupy(_footprint, _origin, _type);
        PlacedRoomRegistry.Instance.Register(_type, _footprint, _origin);
        _factory?.Create(_type, _footprint, _origin);
        GameEventsRelay.Instance.RaiseRoomPlaced(_type);
    }
}