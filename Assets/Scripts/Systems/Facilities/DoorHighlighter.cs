// Spawns/clears clickable markers at free doors that have open space to build into.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class DoorHighlighter
{
    private readonly GameObject _markerPrefab;
    private readonly List<GameObject> _markers = new List<GameObject>();

    public DoorHighlighter(GameObject markerPrefab)
        => _markerPrefab = markerPrefab;
    
    public void Show()
    {
        Hide();
        if (!_markerPrefab || !PlacedRoomRegistry.Exists || !GuildGrid.Exists)
            return;
        
        foreach (var room in PlacedRoomRegistry.Instance.Rooms)
        {
            if (!room.footprint)
                continue;
            foreach (var dp in room.footprint.Doors)
            {
                var key = DoorGeometry.KeyOf(dp, room.origin);
                if (PlacedRoomRegistry.Instance.IsDoorUsed(key)) continue;

                var targetTile = key.tile + DoorGeometry.OffsetFor(key.edge);
                if (!GuildGrid.Instance.IsFree(targetTile))
                    continue;

                var pos = GuildGrid.Instance.TileToWorld(key.tile) + EdgeDir(key.edge);
                var go = Object.Instantiate(_markerPrefab, pos, Quaternion.identity);
                go.GetComponent<DoorMarker>()?.Bind(key, pos);
                _markers.Add(go);
            }
        }
    }
    
    public void Hide()
    {
        foreach (var m in _markers.Where(m => m))
            Object.Destroy(m);
        _markers.Clear();
    }
    
    private static Vector3 EdgeDir(TileEdge edge) => edge switch
    {
        TileEdge.North => Vector3.forward,
        TileEdge.South => Vector3.back,
        TileEdge.East  => Vector3.right,
        TileEdge.West  => Vector3.left,
        _ => Vector3.zero
    };
}
