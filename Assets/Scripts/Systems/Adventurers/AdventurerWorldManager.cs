// Spawns and despawns AdventurerWorldObject instances in response to AdventurerManager events.
// Uses an ordered list of Transform spawn points; cycles through them when more adventurers arrive than there are points.
// Falls back to X-axis spacing if none are assigned.

using System.Collections.Generic;
using UnityEngine;

public class AdventurerWorldManager : MonoBehaviour
{
    #region Identity

    [Header("Prefab")]
    [Tooltip("The AdventurerWorldObject prefab instantiated for each arriving adventurer.")]
    [SerializeField]
    private AdventurerWorldObject adventurerPrefab;

    [Tooltip("QuestConfig asset — passed to world objects for rank colour lookup.")] [SerializeField]
    private QuestConfig questConfig;

    [Header("Spawn Setup")]
    [Tooltip("Parent transform for spawned objects. Keeps the hierarchy clean. " +
             "Leave null to parent to this manager.")]
    [SerializeField]
    private Transform spawnParent;

    [Tooltip("Ordered spawn points. Objects cycle through these as adventurers arrive. " +
             "If empty, objects space out along the X axis using fallback spacing.")]
    [SerializeField]
    private Transform[] spawnPoints;

    [Tooltip("World-unit spacing between objects when no spawn points are assigned.")] [SerializeField, Min(0.5f)]
    private float fallbackSpace = 2f;

    private readonly Dictionary<string, AdventurerWorldObject> _worldObjects = new();

    private int _spawnCounter;

    #endregion

    #region Lifecycle

    private void OnEnable()
    {
        if (!AdventurerManager.Instance)
            return;
        AdventurerManager.Instance.OnAdventurerArrived += HandleAdventurerArrived;
        AdventurerManager.Instance.OnAdventurerLeveledUp += HandleAdventurerChanged;
        AdventurerManager.Instance.OnAdventurerRankUp += HandleAdventurerChanged;
    }

    private void OnDisable()
    {
        if (!AdventurerManager.Instance)
            return;
        AdventurerManager.Instance.OnAdventurerArrived -= HandleAdventurerArrived;
        AdventurerManager.Instance.OnAdventurerLeveledUp -= HandleAdventurerChanged;
        AdventurerManager.Instance.OnAdventurerRankUp -= HandleAdventurerChanged;
    }

    #endregion

    #region Event Handlers
    private void HandleAdventurerArrived(AdventurerData adventurer)
    {
        if (!adventurerPrefab)
        {
            Debug.LogWarning("[AdventurerWorldManager] AdventurerWorldObject prefab not assigned.");
            return;
        }

        var pos = GetNextSpawnPoint();
        var parent = spawnParent ? spawnParent : transform;
        var worldObject = Instantiate(
            adventurerPrefab,
            pos,
            Quaternion.identity,
            parent
        );
        worldObject.Initialize(adventurer, questConfig);
        _worldObjects[adventurer.Id] = worldObject;
        _spawnCounter++;
    }

    private void HandleAdventurerChanged(AdventurerData adventurer)
    {
        if (_worldObjects.TryGetValue(adventurer.Id, out var worldObject))
            worldObject.Refresh();
    }
    #endregion
    
    #region Helpers
    private Vector3 GetNextSpawnPoint()
    {
        if (spawnPoints is { Length: > 0 })
        {
            var point = spawnPoints[_spawnCounter % spawnPoints.Length];
            return point ? point.position : Vector3.zero;
        }
        return new Vector3(_spawnCounter * fallbackSpace, 0f, 0f);
    }
    #endregion
}