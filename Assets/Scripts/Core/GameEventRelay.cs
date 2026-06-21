// All game-wide signals are declared here as UnityEvent fields.
// Managers RAISE events by calling GameEventRelay.Instance.OnXxx.Invoke(...).
// Listeners SUBSCRIBE via GameEventRelay.Instance.OnXxx.AddListener(...).

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
    
    #region Progression Events
    // Fires on every xp value change.
    public readonly UnityEvent<int, int> OnProgressionXpChanged = new();
    // Fires only when the rank changes.
    public readonly UnityEvent<int> OnProgressionRankChanged = new();
    #endregion
    
    #region Adventurer Events
    // (adventurer) — fires when a new adventurer is created and registered to the roster.
    public readonly UnityEvent<AdventurerData> OnAdventurerCreated = new();
    // (adventurer) — fires whenever an adventurer gains a level.
    public readonly UnityEvent<AdventurerData> OnAdventurerLeveledUp = new();
    // (adventurer) — fires the first time an adventurer crosses the rank-up point threshold.
    // Promotion itself is deferred until the Quest system calls AdventurerManager.PromoteRank().
    public readonly UnityEvent<AdventurerData> OnAdventurerRankUpEligible = new();
    // (adventurer) — fires after an adventurer's rank is actually promoted.
    public readonly UnityEvent<AdventurerData> OnAdventurerRankedUp = new();
    // (adventurer) — fires whenever an adventurer's gold balance changes.
    public readonly UnityEvent<AdventurerData> OnAdventurerGoldChanged = new();
    // (adventurer) — fires when an adventurer is removed from the roster.
    public readonly UnityEvent<AdventurerData> OnAdventurerRemoved = new();
    // Fires whenever the roster changes in any way (created, leveled, ranked, removed).
    public readonly UnityEvent OnRosterChanged = new();
    #endregion
    
    #region Class System Events
    // (classData) — fires when a class becomes newly unlocked (by rank or by training).
    public readonly UnityEvent<ClassData> OnClassUnlocked = new();
    #endregion
    
    #region Party Events
    // (party, reason) — fires on any party composition or status change.
    public readonly UnityEvent<PartyData, PartyChangeReason> OnPartyChanged = new();
    // (party) — fires every time a quest result is recorded against a temporary party's trial
    // but the trial isn't decided yet. Lets UI show "2/3 quests, 1 win" progress.
    public readonly UnityEvent<PartyData> OnPartyTrialProgress = new();
    #endregion
}