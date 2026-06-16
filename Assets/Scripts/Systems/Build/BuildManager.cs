// Singleton that owns all placed room instances and drives the build mode state machine.
// Responsibilities: toggling build mode, deducting gold, spawning RoomInstances,
// ticking construction progress, applying/revoking capability and stat-boost effects,
// and broadcasting events to the UI and other systems.
// State Machine (Observer-notified):
//   Normal ──[B key / toggle]──► BuildMode.
//   BuildMode ──[B key / toggle or Escape while no popup open]──► Normal.
// Pattern notes:
//   - Singleton: single authoritative source of build state.
//   - Observer: events let UI react without coupling to this manager.
//   - State: an explicit BuildModeActive flag drives which input is live.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }
    
    #region Inspector
    [Header("Configuration")]
    [Tooltip("Build system config asset (room pool, stat caps, etc.).")]
    [SerializeField] private BuildConfig buildConfig;

    [Header("Input")]
    [Tooltip("Action that toggles build mode. Bind to B in the Gameplay map.")]
    [SerializeField] private InputActionReference toggleBuildModeAction;

    [Tooltip("The 'Building' action map from GameInput.input-actions. " +
             "Enabled while build mode is active.")]
    [SerializeField] private InputActionAsset inputActionAsset;
    #endregion
    
    #region Runtime State
    // All rooms that have been confirmed for construction (including already-built ones).
    private readonly List<RoomInstance> _rooms = new();
    // Tracks the start hour for each room so Tick() can compute progress correctly.
    private readonly Dictionary<string, float> _roomStartHours = new();
    // Whether the player is currently in build mode.
    private bool _buildModeActive;
    // Cached action map reference.
    private InputActionMap _buildingActionMap;
    #endregion
    
    #region Public Properties
    public bool BuildModeActive => _buildModeActive;
    public BuildConfig Config => buildConfig;
    public IReadOnlyList<RoomInstance> Rooms => _rooms;
    #endregion
    
    #region Lifecycle
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!buildConfig)
            Debug.LogError("[BuildManager] BuildConfig not assigned in the inspector.");

        if (inputActionAsset)
        {
            _buildingActionMap = inputActionAsset.FindActionMap("Building", throwIfNotFound: false);
            if (_buildingActionMap == null)
                Debug.LogWarning("[BuildManager] 'Building' action map not found in InputActionAsset.");
        }
    }

    private void OnEnable()
    {
        if (toggleBuildModeAction?.action != null)
        {
            toggleBuildModeAction.action.Enable();
            toggleBuildModeAction.action.performed += HandleToggleBuildMode;
        }
        
        GameEventRelay.Instance.OnHourChanged.AddListener(HandleHourChanged);
    }

    private void OnDisable()
    {
        if (toggleBuildModeAction?.action != null)
        {
            toggleBuildModeAction.action.performed -= HandleToggleBuildMode;
            toggleBuildModeAction.action.Disable();
        }
        
        GameEventRelay.Instance.OnHourChanged.RemoveListener(HandleHourChanged);
    }
    #endregion
    
    #region Build Mode Toggle
    // Called by input or external systems (e.g., a HUD button).
    public void ToggleBuildMode()
    {
        // Cannot enter build mode while a screen is open.
        if (!_buildModeActive && InteractionManager.Instance && InteractionManager.Instance.IsScreenOpen)
            return;

        SetBuildMode(!_buildModeActive);
    }

    private void SetBuildMode(bool active)
    {
        _buildModeActive = active;

        // Disable normal world interaction while building so doors don't compete with other props.
        // Camera is intentionally NOT disabled — the player can still orbit.
        InteractionManager.Instance?.SetInteractionEnabled(!active);

        if (active)
            _buildingActionMap?.Enable();
        else
            _buildingActionMap?.Disable();

        GameEventRelay.Instance.OnBuildModeChanged?.Invoke(active);
    }

    private void HandleToggleBuildMode(InputAction.CallbackContext ctx)
        => ToggleBuildMode();
    #endregion
    
    #region Construction
    // Called by the confirmation popup UI after the player confirms.
    // Returns false and does NOT start building if the guild cannot afford it.
    public bool TryBuildRoom(RoomDefinition definition)
    {
        if (!definition)
        {
            Debug.LogWarning("[BuildManager] TryBuildRoom called with null definition.");
            return false;
        }

        // Afford check against guild treasury.
        if (!QuestManager.Instance || QuestManager.Instance.GuildFunds < definition.GoldCost)
        {
            Debug.Log($"[BuildManager] Cannot afford '{definition.RoomName}' " +
                      $"(cost {definition.GoldCost}, funds {QuestManager.Instance?.GuildFunds ?? 0}).");
            return false;
        }

        // Deduct cost. QuestManager owns the treasury.
        QuestManager.Instance.SpendGuildFunds(definition.GoldCost);

        var startHour = GetCurrentGameHours();
        var instance = new RoomInstance(definition, startHour);

        _rooms.Add(instance);
        _roomStartHours[instance.InstanceId] = startHour;

        GameEventRelay.Instance.OnRoomQueued?.Invoke(instance);
        GameEventRelay.Instance.OnRoomsChanged?.Invoke();
        return true;
    }

    private void HandleHourChanged(int hour)
    {
        var currentHour = GetCurrentGameHours();
        TickConstruction(currentHour);
    }

    // Advances all under-construction rooms by one-hour tick.
    private void TickConstruction(float currentHour)
    {
        foreach (var room in _rooms)
        {
            if (room.IsBuilt) continue;

            var startHour = _roomStartHours.GetValueOrDefault(room.InstanceId, currentHour);
            var justCompleted = room.Tick(currentHour, startHour);

            if (justCompleted)
            {
                ApplyRoomCapabilities(room);
                GameEventRelay.Instance.OnRoomCompleted?.Invoke(room);
            }
            else
            {
                GameEventRelay.Instance.OnRoomProgressUpdated?.Invoke(room);
            }
        }

        GameEventRelay.Instance.OnRoomsChanged?.Invoke();
    }
    #endregion
    
    #region Capabilities
    // Applies stat boosts and capability flags to the adventurer system when a room finishes.
    // Actual per-adventurer stat recalculation is triggered via AdventurerManager.
    private void ApplyRoomCapabilities(RoomInstance room)
    {
        var caps = room.Definition.Capabilities;

        // Stat boosts: notify AdventurerManager to recalculate all adventurer stats.
        if (caps.hpBonus > 0f || caps.damageBonus > 0f || caps.speedBonus > 0f)
            SoloAdventurerManager.Instance?.RecalculateAllStatBoosts();

        // Bed slots: notify AdventurerManager so it can assign homeless adventurers.
        if (caps.bedSlots > 0)
            SoloAdventurerManager.Instance?.OnBedsAdded(caps.bedSlots);

        Debug.Log($"[BuildManager] Room '{room.Definition.RoomName}' completed and capabilities applied.");
    }

    // Returns the cumulative stat bonuses from all BUILT rooms, clamped to config caps.
    public (float hp, float dmg, float spd) GetTotalStatBoosts()
    {
        float hp = 0f, dmg = 0f, spd = 0f;
        foreach (var caps in from room in _rooms
                 where room.IsBuilt select room.Definition.Capabilities)
        {
            hp  += caps.hpBonus;
            dmg += caps.damageBonus;
            spd += caps.speedBonus;
        }

        if (buildConfig)
        {
            hp  = Mathf.Min(hp,  buildConfig.MaxHpBonus);
            dmg = Mathf.Min(dmg, buildConfig.MaxDamageBonus);
            spd = Mathf.Min(spd, buildConfig.MaxSpeedBonus);
        }

        return (hp, dmg, spd);
    }

    // Returns total number of bed slots from all completed rooms.
    public int GetTotalBedSlots()
        => _rooms.Where(room => room.IsBuilt).Sum(room => room.Definition.Capabilities.bedSlots);

    // Returns true if any completed room provides food and drink.
    public bool HasFoodAndDrink()
        => _rooms.Any(room => room.IsBuilt && room.Definition.Capabilities.providesFoodAndDrink);

    // Returns true if any completed room provides training.
    public bool HasTraining()
        => _rooms.Any(room => room.IsBuilt && room.Definition.Capabilities.providesTraining);
    #endregion
    
    #region Helpers
    private float GetCurrentGameHours()
        => TimeManager.Instance.GetTotalGameHours();
    #endregion
}