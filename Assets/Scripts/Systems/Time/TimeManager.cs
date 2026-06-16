// Singleton that drives all in-game time progression.
// Raises all time events through GameEventRelay instead of direct C# events,
// guaranteeing subscribers never miss events due to singleton initialization order.
// Input actions for pause / timescale are managed here.

using UnityEngine;
using UnityEngine.InputSystem;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }
    
    #region Time Configuration
    [Header("Time Configuration")]
    [Tooltip("How many real-world seconds equal one in-game minute. Lower = faster days.")]
    [SerializeField] private float realSecondsPerGameMinute = 1f;

    [Tooltip("In-game hour to start at (0–23).")]
    [SerializeField, Range(0, 23)] private int startHour = 6;

    [Tooltip("In-game minute to start at (0–59).")]
    [SerializeField, Range(0, 59)] private int startMinute;

    [Tooltip("Starting day of the month (1-based).")]
    [SerializeField, Min(1)] private int startDay = 1;

    [Tooltip("Starting month (1-based).")]
    [SerializeField, Range(1, 12)] private int startMonth = 1;

    [Tooltip("Starting year.")]
    [SerializeField, Min(1)] private int startYear = 1;

    [Tooltip("How many days are in each month.")]
    [SerializeField, Min(1)] private int daysPerMonth = 30;

    [Tooltip("How many months are in a year. Divisible by 4 keeps seasons even.")]
    [SerializeField, Range(4, 24)] private int monthsPerYear = 12;
    #endregion
    
    #region Time Scale
    [Header("Time Scale")]
    [Tooltip("Multiplier applied on top of realSecondsPerGameMinute.")]
    [SerializeField, Min(0f)] private float timeScale = 1f;

    [Tooltip("Maximum time scale reachable through input.")]
    [SerializeField, Min(1f)] private float maxTimeScale = 10f;

    [Tooltip("How much each key press increments or decrements the time scale.")]
    [SerializeField, Min(0.5f)] private float timeScaleStep = 1f;
    #endregion
    
    #region Input Actions (Gameplay Map)
    [Header("Input - Gameplay Map")]
    [SerializeField] private InputActionReference pauseToggleAction;
    [SerializeField] private InputActionReference increaseTimeScaleAction;
    [SerializeField] private InputActionReference decreaseTimeScaleAction;
    #endregion
    
    #region Internal State
    private int _minute, _hour, _day, _month, _year;
    private bool _isPaused;

    // Accumulates fractional minutes between frames so no time is lost at high frame rates.
    private float _minuteAccumulator;

    // Tracks the season during the previous tick to detect season boundaries.
    private Season _lastKnownSeason;
    #endregion
    
    #region Public Read-Only Properties
    public int DaysPerMonth => daysPerMonth;
    public int MonthsPerYear => monthsPerYear;
    public int Minute => _minute;
    public int Hour => _hour;
    public int Day => _day;
    public int Month => _month;
    public int Year => _year;
    public bool IsPaused => _isPaused;
    public float TimeScale => timeScale;
    
    // Normalized position within the current day: 0.0 = midnight, 1.0 = next midnight.
    // Safe to sample every frame for smooth lighting interpolation.
    public float NormalizedDayTime => (_hour + _minute / 60f) / 24f;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _minute = startMinute;
        _hour = startHour;
        _day = startDay;
        _month = startMonth;
        _year = startYear;
        _lastKnownSeason = GetCurrentSeason();
        GameEventRelay.Instance.onMinuteChanged.Invoke(_hour, _minute);
    }

    private void OnEnable()
    {
        EnableAndSubscribe(pauseToggleAction, OnPauseTogglePerformed);
        EnableAndSubscribe(increaseTimeScaleAction, OnIncreasePerformed);
        EnableAndSubscribe(decreaseTimeScaleAction, OnDecreasePerformed);
    }

    private void OnDisable()
    {
        DisableAndUnsubscribe(pauseToggleAction, OnPauseTogglePerformed);
        DisableAndUnsubscribe(increaseTimeScaleAction, OnIncreasePerformed);
        DisableAndUnsubscribe(decreaseTimeScaleAction, OnDecreasePerformed);
    }

    private void Update()
    {
        if (_isPaused || timeScale <= 0f || realSecondsPerGameMinute <= 0f)
            return;

        _minuteAccumulator += Time.deltaTime * timeScale / realSecondsPerGameMinute;

        // Drain whole minutes; while-loop handles multiple minutes in one frame at extreme scale.
        while (_minuteAccumulator >= 1f)
        {
            _minuteAccumulator -= 1f;
            TickMinute();
        }
    }
    #endregion
    
    #region Tick Logic
    private void TickMinute()
    {
        _minute++;
        var hourChanged = false;
        var dayChanged  = false;
        var monthChanged = false;
        var yearChanged  = false;

        if (_minute >= 60)
        {
            _minute = 0;
            _hour++;
            hourChanged = true;
        }
        if (_hour >= 24)
        {
            _hour = 0;
            _day++;
            dayChanged = true;
        }
        if (_day > daysPerMonth)
        {
            _day = 1;
            _month++;
            monthChanged = true;
        }
        if (_month > monthsPerYear)
        {
            _month = 1;
            _year++;
            yearChanged = true;
        }

        // Guard: relay must exist. It always does because GameEventRelay's Awake
        // runs before TimeManager's first tick (same frame, earlier script order).
        if (!GameEventRelay.Instance)
            return;

        // Fire granular events from most specific to least specific so listeners that
        // unsubscribe in response don't miss earlier events in the same tick.
        GameEventRelay.Instance.onMinuteChanged.Invoke(_hour, _minute);

        if (hourChanged)
            GameEventRelay.Instance.onHourChanged.Invoke(_hour);
        if (dayChanged)
            GameEventRelay.Instance.onDayChanged.Invoke(_day);
        if (monthChanged)
            GameEventRelay.Instance.onMonthChanged.Invoke(_month);
        if (yearChanged)
            GameEventRelay.Instance.onYearChanged.Invoke(_year);

        // Season check — only fire when the season actually changes.
        var currentSeason = GetCurrentSeason();
        if (currentSeason != _lastKnownSeason)
        {
            _lastKnownSeason = currentSeason;
            GameEventRelay.Instance.onSeasonChanged.Invoke(currentSeason);
        }
    }
    #endregion
    
    #region Public Time Queries
    // Returns the current time-of-day category based on the hour.
    public TimeOfDay GetCurrentTimeOfDay()
    {
        return _hour switch
        {
            >= 0 and < 5 => TimeOfDay.Midnight,
            >= 5 and < 7 => TimeOfDay.Dawn,
            >= 7 and < 12 => TimeOfDay.Morning,
            >= 12 and < 13 => TimeOfDay.Noon,
            >= 13 and < 17 => TimeOfDay.Afternoon,
            >= 17 and < 19 => TimeOfDay.Evening,
            >= 19 and < 21 => TimeOfDay.Dusk,
            _ => TimeOfDay.Night
        };
    }

    // Returns the current season based on the month.
    public Season GetCurrentSeason()
    {
        var seasonIndex = ((_month - 1) / (monthsPerYear / 4)) % 4;
        return (Season)seasonIndex;
    }

    // Human-readable date string for UI display.
    public string GetFormattedDate()
        => $"Day {_day}, Month {_month}, Year {_year}";

    // Total elapsed in-game hours from Year 1 / Month 1 / Day 1 / 00:00.
    // Used by all managers for deadline and timer arithmetic. Centralised here
    // to eliminate the duplicated helper that previously lived in each manager.
    public float GetTotalGameHours()
        => (_year - 1) * monthsPerYear * daysPerMonth * 24f
           + (_month - 1) * daysPerMonth  * 24f
           + (_day - 1) * 24f
           + _hour
           + _minute / 60f;
    #endregion
    
    #region Pause & Timescale
    // Toggles the paused state and broadcasts the change through the relay.
    public void SetPaused(bool paused)
    {
        _isPaused = paused;
        GameEventRelay.Instance?.onPauseChanged.Invoke(_isPaused);
    }

    public void SetTimeScale(float scale)
        => timeScale = Mathf.Clamp(scale, 0f, maxTimeScale);
    #endregion
    
    #region Input Handlers
    private void OnPauseTogglePerformed(InputAction.CallbackContext _)
        => SetPaused(!_isPaused);

    private void OnIncreasePerformed(InputAction.CallbackContext _)
        => timeScale = Mathf.Min(timeScale + timeScaleStep, maxTimeScale);

    private void OnDecreasePerformed(InputAction.CallbackContext _)
        => timeScale = Mathf.Max(timeScale - timeScaleStep, 0f);
    #endregion
    
    #region Input Helpers
    private static void EnableAndSubscribe(InputActionReference actionRef, System.Action<InputAction.CallbackContext> handler)
    {
        if (!actionRef) return;
        actionRef.action.Enable();
        actionRef.action.performed += handler;
    }

    private static void DisableAndUnsubscribe(InputActionReference actionRef, System.Action<InputAction.CallbackContext> handler)
    {
        if (!actionRef) return;
        actionRef.action.performed -= handler;
        actionRef.action.Disable();
    }
    #endregion
}