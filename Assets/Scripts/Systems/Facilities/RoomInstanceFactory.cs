// Factory: instantiates a RoomInstance at a footprint's grid origin and binds it to its facility data.

using UnityEngine;

public class RoomInstanceFactory
{
    private readonly GameObject _roomInstancePrefab;

    public RoomInstanceFactory(GameObject roomInstancePrefab)
        => _roomInstancePrefab = roomInstancePrefab;

    public RoomInstance Create(FacilityType type, RoomFootprint footprint, TileCoord origin)
    {
        var data = FacilityController.Exists ? FacilityController.Instance.Get(type)?.Data : null;
        if (!data)
            return null;

        var pos = GuildGrid.Instance.TileToWorld(origin);
        var go = Object.Instantiate(_roomInstancePrefab, pos, Quaternion.identity);
        var instance = go.GetComponent<RoomInstance>();
        instance.Initialize(type, data);
        return instance;
    }
}