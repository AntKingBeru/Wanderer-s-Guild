// Spawns and despawns AdventurerWorldObject instances.
// Spawns 2 starter adventurers on Day 1 at the configured start hour (before the regular factory cycle takes over).
// Each spawned object receives a patrol center from the designated wander area.
// Bridges SoloAdventurerManager's OnApplicationSubmitted relay event to the
// relevant world object so it walks to the board.

using System.Collections.Generic;
using UnityEngine;

public class AdventurerWorldManager : MonoBehaviour
{
    #region Identity
    [Header("Prefab")]
    [Tooltip("The AdventurerWorldObject prefab instantiated for each arriving adventurer.")]
    [SerializeField] private AdventurerWorldObject adventurerPrefab;

    [Tooltip("QuestConfig asset — passed to world objects for rank colour lookup.")]
    [SerializeField] private QuestConfig questConfig;

    [Header("Spawn Setup")]
    [Tooltip("Parent transform for spawned objects. Keeps the hierarchy clean. " +
             "Leave null to parent to this manager.")]
    [SerializeField] private Transform spawnParent;

    [Tooltip("Ordered spawn points. Objects cycle through these as adventurers arrive. " +
             "If empty, objects space out along the X axis using fallback spacing.")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("World-unit spacing between objects when no spawn points are assigned.")]
    [SerializeField, Min(0.5f)] private float fallbackSpace = 2f;
    
    [Header("Patrol Area")]
    [Tooltip("Centre of the area adventurers wander when idle. " +
             "All adventurers share this center; radius is set on AdventurerNavigationController.")]
    [SerializeField] private Transform patrolCenter;

    [Header("Starter Adventurers")]
    [Tooltip("In-game hour on Day 1 at which the two starter adventurers are spawned. " +
             "Should fall within the application window (default 7).")]
    [SerializeField, Range(0, 23)] private int starterSpawnHour = 7;

    // Whether the two starter adventurers have already been spawned this session.
    private bool _startersSpawned;
    private readonly Dictionary<string, AdventurerWorldObject> _worldObjects = new();
    private int _spawnCounter;
    #endregion

    #region Lifecycle
    private void OnEnable()
    {
        if (!AdventurerManager.Instance)
            return;
        GameEventRelay.Instance.OnAdventurerArrived.AddListener(HandleAdventurerArrived);
        GameEventRelay.Instance.OnAdventurerLeveledUp.AddListener(HandleAdventurerChanged);
        GameEventRelay.Instance.OnAdventurerRankUp.AddListener(HandleAdventurerChanged);
        // Listen for hour ticks to trigger starter spawns at the right moment.
        GameEventRelay.Instance.OnHourChanged.AddListener(HandleHourChanged);
        GameEventRelay.Instance.OnAdventurerApplicationSubmitted.AddListener(NotifyApplicationSubmitted);
    }

    private void OnDisable()
    {
        if (!AdventurerManager.Instance)
            return;
        GameEventRelay.Instance.OnAdventurerArrived.RemoveListener(HandleAdventurerArrived);
        GameEventRelay.Instance.OnAdventurerLeveledUp.RemoveListener(HandleAdventurerChanged);
        GameEventRelay.Instance.OnAdventurerRankUp.RemoveListener(HandleAdventurerChanged);
        GameEventRelay.Instance.OnHourChanged.RemoveListener(HandleHourChanged);
        GameEventRelay.Instance.OnAdventurerApplicationSubmitted.RemoveListener(NotifyApplicationSubmitted);
    }
    #endregion

    #region Event Handlers
    // On Day 1 at the configured hour, force-spawn 2 adventurers through the normal factory.
    // After that, the regular interval-based factory takes over.
    private void HandleHourChanged(int hour)
    {
        if (_startersSpawned)
            return;
        if (!TimeManager.Instance)
            return;
        if (TimeManager.Instance.Day != 1)
            return;
        if (hour < starterSpawnHour)
            return;

        _startersSpawned = true;

        var sam = SoloAdventurerManager.Instance;
        if (!sam)
        {
            Debug.LogWarning("[AdventurerWorldManager] SoloAdventurerManager not found; " +
                             "cannot spawn starter adventurers.");
            return;
        }

        // Spawn 2 adventurers immediately via the factory.
        // SpawnStarterAdventurer is a new public method added to SoloAdventurerManager (see note below).
        sam.SpawnStarterAdventurer();
        sam.SpawnStarterAdventurer();
    }
    
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
        worldObject.Initialize(adventurer, patrolCenter);
        _worldObjects[adventurer.Id] = worldObject;
        _spawnCounter++;
    }

    private void HandleAdventurerChanged(AdventurerData adventurer)
    {
        if (_worldObjects.TryGetValue(adventurer.Id, out var worldObject))
            worldObject.Refresh();
    }
    
    // Called by SoloAdventurerManager (bridged through GameEventRelay) when an
    // application is submitted so the relevant world object walks to the board.
    public void NotifyApplicationSubmitted(string adventurerId)
    {
        if (_worldObjects.TryGetValue(adventurerId, out var obj))
            obj.NotifyApplicationSubmitted();
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