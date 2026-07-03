// Factory: instantiates and initializes an in-world adventurer visual (agent, movement, billboard).

using UnityEngine;

public class AdventurerVisualFactory
{
    private readonly GameObject _prefab;
    
    public AdventurerVisualFactory(GameObject prefab)
        => _prefab = prefab;
    
    public AdventurerVisual Create(Adventurer adventurer, Sprite classSprite)
    {
        var spawn = WorldAnchors.Instance.SpawnPoint;
        var pos = spawn ? spawn.position : Vector3.zero;

        var go = Object.Instantiate(_prefab, pos, Quaternion.identity);
        var visual = go.GetComponent<AdventurerVisual>();
        visual.Bind(adventurer, classSprite);
        return visual;
    }
}