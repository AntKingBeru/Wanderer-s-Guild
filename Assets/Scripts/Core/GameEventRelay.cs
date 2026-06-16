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
    public readonly UnityEvent<int, int> OnMinuteChanged = new();
    // (hour) — fires once per in-game hour.
    public readonly UnityEvent<int> OnHourChanged = new();
    // (day) — fires after midnight rollover; month/year already updated.
    public readonly UnityEvent<int> OnDayChanged = new();
    // (month) — fires when the month increments.
    public readonly UnityEvent<int> OnMonthChanged = new();
    // (season) — fires when the season changes.
    public readonly UnityEvent<Season> OnSeasonChanged = new();
    // (year) — fires when the year increments.
    public readonly UnityEvent<int> OnYearChanged = new();
    // (isPaused) — fires when time is paused or unpaused.
    public readonly UnityEvent<bool> OnPauseChanged = new();
    #endregion
    
    #region Quest Events
    // Fires when the available request pool changes (draw / expiry).
    public readonly UnityEvent OnAvailableRequestsChanged = new();
    // Fires when the unposted quest list changes.
    public readonly UnityEvent OnUnpostedQuestsChanged = new();
    // Fires when any board slot changes (added, dispatched, expired).
    public readonly UnityEvent OnBoardChanged = new();
    // (application) — fires when a new application is submitted to a posted quest.
    public readonly UnityEvent<QuestApplication> OnApplicationSubmitted = new();
    // (application) — fires when a single application is manually rejected by the player.
    public readonly UnityEvent<QuestApplication> OnApplicationRejected = new();
    // (quest) — fires on any quest status transition.
    public readonly UnityEvent<QuestData> OnQuestStatusChanged = new();
    // (newFunds) — fires whenever the guild treasury balance changes.
    public readonly UnityEvent<int> OnGuildFundsChanged = new();
    #endregion
    
    #region Advneturer Events
    // (adventurer) — fires when a new adventurer registers at the guild.
    public readonly UnityEvent<AdventurerData> OnAdventurerArrived = new();
    // (adventurer) — fires when any adventurer gains a level.
    public readonly UnityEvent<AdventurerData> OnAdventurerLeveledUp = new();
    // (adventurer) — fires when an adventurer first crosses the rank-up point threshold.
    public readonly UnityEvent<AdventurerData> OnRankUpEligibilityGained = new();
    // (adventurer) — fires after a successful rank-up quest.
    public readonly UnityEvent<AdventurerData> OnAdventurerRankUp = new();
    // (adventurer) — fires after a failed rank-up quest.
    public readonly UnityEvent<AdventurerData> OnAdventurerRankUpFailed = new();
    // (application) — fires when a rank-up application is created.
    public readonly UnityEvent<RankUpApplicationData> OnRankUpApplicationCreated = new();
    // (application) — fires when a rank-up application is approved or rejected.
    public readonly UnityEvent<RankUpApplicationData> OnRankUpApplicationResolved = new();
    // Fires whenever the adventurer roster changes (arrival, level, rank, etc.)
    public readonly UnityEvent OnRosterChanged = new();
    // Fires whenever an adventurer submits a quest application, so their world object walks to the board.
    public readonly UnityEvent<string> OnAdventurerApplicationSubmitted = new();
    #endregion
    
    #region Party Events
    // (party, reason) — fires on any party composition or status change.
    public readonly UnityEvent<PartyData, PartyChangeReason> OnPartyChanged = new();
    #endregion
    
    #region Build Events
    // Fires when build mode is entered or exited.
    public readonly UnityEvent<bool> OnBuildModeChanged = new();
    // Fires when a new room is queued for construction.
    public readonly UnityEvent<RoomInstance> OnRoomQueued = new();
    // Fires each hour tick for rooms under construction (progress updated).
    public readonly UnityEvent<RoomInstance> OnRoomProgressUpdated = new();
    // Fires the moment a room finishes construction.
    public readonly UnityEvent<RoomInstance> OnRoomCompleted = new();
    // Fires whenever the room list changes (for UI list refresh).
    public readonly UnityEvent OnRoomsChanged = new();
    // Fired when a door is clicked in build mode. Carries screen-space position for menu placement.
    public readonly UnityEvent<BuildDoor, Vector2> OnDoorClicked = new();
    #endregion

    #region Reputation Events
    // (newValue) — fires on every reputation value change.
    public readonly UnityEvent<int> OnReputationChanged = new();
    // (newLevel) — fires only when the reputation tier changes.
    public readonly UnityEvent<ReputationLevel> OnReputationLevelChanged = new();
    #endregion
}