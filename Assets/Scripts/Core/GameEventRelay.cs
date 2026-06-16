// Central event bus using the Observer pattern via UnityEvents.
// All game-wide signals are declared here as UnityEvent fields.
// Managers RAISE events by calling GameEventRelay.Instance.OnXxx.Invoke(...).
// Listeners SUBSCRIBE via GameEventRelay.Instance.OnXxx.AddListener(...).
// Because this is a DontDestroyOnLoad singleton initialized in Awake,
// it is guaranteed to exist before any other manager's OnEnable fires,
// completely eliminating the cross-singleton subscription race condition.

using UnityEngine;
using UnityEngine.Events;

public class GameEventRelay : MonoBehaviour
{
    public static GameEventRelay Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // All fire AFTER the time state has fully rolled over for consistent reads.
    #region Time Events
    // (hour, minute) — fires every in-game minute.
    public readonly UnityEvent<int, int> onMinuteChanged = new();
    // (hour) — fires once per in-game hour.
    public readonly UnityEvent<int> onHourChanged = new();
    // (day) — fires after midnight rollover; month/year already updated.
    public readonly UnityEvent<int> onDayChanged = new();
    // (month) — fires when the month increments.
    public readonly UnityEvent<int> onMonthChanged = new();
    // (season) — fires when the season changes.
    public readonly UnityEvent<Season> onSeasonChanged = new();
    // (year) — fires when the year increments.
    public readonly UnityEvent<int> onYearChanged = new();
    // (isPaused) — fires when time is paused or unpaused.
    public readonly UnityEvent<bool> onPauseChanged = new();
    #endregion
    
    #region Quest Events
    // Fires when the available request pool changes (draw / expiry).
    public readonly UnityEvent onAvailableRequestsChanged = new();
    // Fires when the unposted quest list changes.
    public readonly UnityEvent onUnpostedQuestsChanged = new();
    // Fires when any board slot changes (added, dispatched, expired).
    public readonly UnityEvent onBoardChanged = new();
    // (application) — fires when a new application is submitted to a posted quest.
    public readonly UnityEvent<QuestApplication> onApplicationSubmitted = new();
    // (application) — fires when the player manually rejects a single application.
    public readonly UnityEvent<QuestApplication> onApplicationRejected = new();
    // (quest) — fires on any quest status transition.
    public readonly UnityEvent<QuestData> onQuestStatusChanged = new();
    // (newFunds) — fires whenever the guild treasury balance changes.
    public readonly UnityEvent<int> onGuildFundsChanged = new();
    #endregion
    
    #region Advneturer Events
    // (adventurer) — fires when a new adventurer registers at the guild.
    public readonly UnityEvent<AdventurerData> onAdventurerArrived = new();
    // (adventurer) — fires when any adventurer gains a level.
    public readonly UnityEvent<AdventurerData> onAdventurerLeveledUp = new();
    // (adventurer) — fires when an adventurer first crosses the rank-up point threshold.
    public readonly UnityEvent<AdventurerData> onRankUpEligibilityGained = new();
    // (adventurer) — fires after a successful rank-up quest.
    public readonly UnityEvent<AdventurerData> onAdventurerRankUp = new();
    // (adventurer) — fires after a failed rank-up quest.
    public readonly UnityEvent<AdventurerData> onAdventurerRankUpFailed = new();
    // (application) — fires when a rank-up application is created.
    public readonly UnityEvent<RankUpApplicationData> onRankUpApplicationCreated = new();
    // (application) — fires when a rank-up application is approved or rejected.
    public readonly UnityEvent<RankUpApplicationData> onRankUpApplicationResolved = new();
    // Fires whenever the adventurer roster changes (arrival, level, rank, etc.)
    public readonly UnityEvent onRosterChanged = new();
    // Fires whenever an adventurer submits a quest application, so their world object walks to the board.
    public readonly UnityEvent<string> onAdventurerApplicationSubmitted = new();
    #endregion
    
    #region Party Events
    // (party, reason) — fires on any party composition or status change.
    public readonly UnityEvent<PartyData, PartyChangeReason> onPartyChanged = new();
    #endregion
    
    #region Build Events
    // Fires when build mode is entered or exited.
    public readonly UnityEvent<bool> onBuildModeChanged = new();
    // Fires when a new room is queued for construction.
    public readonly UnityEvent<RoomInstance> onRoomQueued = new();
    // Fires each hour tick for rooms under construction (progress updated).
    public readonly UnityEvent<RoomInstance> onRoomProgressUpdated = new();
    // Fires the moment a room finishes construction.
    public readonly UnityEvent<RoomInstance> onRoomCompleted = new();
    // Fires whenever the room list changes (for UI list refresh).
    public readonly UnityEvent onRoomsChanged = new();
    // Fired when a door is clicked in build mode. Carries screen-space position for menu placement.
    public readonly UnityEvent<BuildDoor, Vector2> onDoorClicked = new();
    #endregion

    #region Reputation Events
    // (newValue) — fires on every reputation value change.
    public readonly UnityEvent<int> onReputationChanged = new();
    // (newLevel) — fires only when the reputation tier changes.
    public readonly UnityEvent<ReputationLevel> onReputationLevelChanged = new();
    #endregion
    
    #region Progression Events
    // Fires on every xp value change.
    public readonly UnityEvent<int, int> onProgressionXpChanged = new();
    // Fires only when the rank changes.
    public readonly UnityEvent<int> onProgressionRankChanged = new();
    #endregion

    #region SceneChange Events
    // (new value) - fires on every scene progress change
    public readonly UnityEvent<float> onSceneProgressChanged = new();
    #endregion
}