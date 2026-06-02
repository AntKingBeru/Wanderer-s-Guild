// Singleton that drives all in-game time progression.
// Other systems should subscribe to its events rather than polling it each frame.
// Input actions are managed individually here.
// Call actionMap.Disable() externally to suppress all Gameplay input at once.

using System;
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

    [Tooltip("How many days are in each month. Kept uniform for simplicity.")]
    [SerializeField, Min(1)] private int daysPerMonth = 30;

    [Tooltip("How many months are in a year. Should be divisible by 4 to keep seasons even.")]
    [SerializeField, Range(4, 24)] private int monthsPerYear = 12;
    #endregion
    
    #region Time Scale
    [Header("Time Scale")]
    [Tooltip("Multiplier applied on top of realSecondsPerGameMinute. 1 = normal, 2 = double speed, etc.")]
    [SerializeField, Min(0f)] private float timeScale = 1f;
    
    [Tooltip("Maximum time scale reachable through input.")]
    [SerializeField, Min(1f)] private float maxTimeScale = 10f;
    
    [Tooltip("How much each key press increments or decrements the time scale.")]
    [SerializeField, Min(0.5f)] private float timeScaleStep = 1f;
    #endregion
    
    #region Input Actions (Gameplay Map)
    [Header("Input - Gameplay Map")]
    [Tooltip("Toggles time pause on/off")]
    [SerializeField] private InputActionReference pauseToggleAction;
    
    [Tooltip("Increases time scale by one step")]
    [SerializeField] private InputActionReference increaseTimeScaleAction;
    
    [Tooltip("Decreases time scale by one step")]
    [SerializeField] private InputActionReference decreaseTimeScaleAction;
    #endregion
    
    #region Internal State
    private int _minute, _hour, _day, _month, _year;
    private bool _isPaused;
    
    // Accumulates fractional minutes between frames so no time is lost at high frame rates.
    private float _minuteAccumulator;
    #endregion
    
    #region Public Events
    // All events fire after state has fully rolled over, guaranteeing consistent values.
    public event Action<int, int> OnMinuteChanged; // (minute, hour)
    public event Action<int> OnHourChanged; // (hour)
    public event Action<int> OnDayChanged; // (day)
    public event Action<int> OnMonthChanged; // (month)
    public event Action<Season> OnSeasonChanged; // (season)
    public event Action<int> OnYearChanged; // (year)
    public event Action<bool> OnPauseChanged; // (isPaused)
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
    
    /// <summary>
    /// Normalized position within the current day: 0.0 = midnight, 0.5 = noon, 1.0 = next day midnight.
    /// Updated continuously and safe to sample every frame for smooth lighting interpolation.
    /// </summary>
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
        
        // Accumulate scaled time.
        // One unit = one game minute.
        _minuteAccumulator += Time.deltaTime * timeScale / realSecondsPerGameMinute;
        
        // Drain whole minutes from the accumulator, preserving the remainder.
        // Using a while loop handles the unlikely case where multiple minutes pass in a single frame at extreme scales.
        while (_minuteAccumulator >= 1f)
        {
            _minuteAccumulator -= 1f;
            AdvanceMinute();
        }
    }
    #endregion
    
    #region Time Advancement
    // Each Advance method increments its unit, delegates upward if a threshold is crossed,
    // then fires its event AFTER all corrections are completed.
    private void AdvanceMinute()
    {
        _minute++;
        if (_minute >= 60)
        {
            _minute = 0;
            AdvanceHour();
        }
        OnMinuteChanged?.Invoke(_hour, _minute);
    }

    private void AdvanceHour()
    {
        _hour++;
        if (_hour >= 24)
        {
            _hour = 0;
            AdvanceDay();
        }
        OnHourChanged?.Invoke(_hour);
    }

    private void AdvanceDay()
    {
        _day++;
        if (_day >= daysPerMonth)
        {
            _day = 1;
            AdvanceMonth();
        }
        OnDayChanged?.Invoke(_day);
    }

    private void AdvanceMonth()
    {
        // Capture season before incrementing so we can detect a season boundary crossing.
        var previousSeason = GetCurrentSeason();
        _month++;
        if (_month > monthsPerYear)
        {
            _month = 1;
            AdvanceYear();
        }

        var newSeason = GetCurrentSeason();
        if (newSeason != previousSeason)
            OnSeasonChanged?.Invoke(newSeason);
        
        OnMonthChanged?.Invoke(_month);
    }

    private void AdvanceYear()
    {
        _year++;
        OnYearChanged?.Invoke(_year);
    }
    #endregion
    
    #region Public Query Methods
    /// <summary>
    /// Returns the current season based on the current month.
    /// Requires monthsPerYear to be divisible by 4 for even season boundaries.
    /// </summary>
    public Season GetCurrentSeason()
    {
        var monthsPerSeason = Mathf.Max(1, monthsPerYear / 4);
        var index = Mathf.Clamp((_month - 1) / monthsPerSeason, 0, 3);
        return (Season)index;
    }
    
    /// <summary>
    /// Returns the broad time-of-day category for the current hour.
    /// </summary>
    public TimeOfDay GetCurrentTimeOfDay()
    {
        return _hour switch
        {
            < 5 => TimeOfDay.Midnight,
            < 7 => TimeOfDay.Dawn,
            < 12 => TimeOfDay.Morning,
            < 13 => TimeOfDay.Noon,
            < 17 => TimeOfDay.Afternoon,
            < 19 => TimeOfDay.Evening,
            < 21 => TimeOfDay.Dusk,
            _ => TimeOfDay.Night
        };
    }

    /// <summary>
    /// Returns the current in-game time formatted as "HH:MM".
    /// </summary>
    public string GetFormattedTime() => $"{_hour:D2}:{_minute:D2}";
    
    /// <summary>
    /// Returns a readable date string, e.g. "Day 4, Month 3, Year 2".
    /// </summary>
    public string GetFormattedDate() => $"{(Weekday)(_day - 1)}, {(MonthDisplay)(_month - 1)}, Year {_year}";
    #endregion
    
    #region Public Control Methods
    public void SetPaused(bool paused)
    {
        _isPaused = paused;
        OnPauseChanged?.Invoke(_isPaused);
    }

    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Clamp(scale, 0f, maxTimeScale);
    }
    #endregion
    
    #region Input Callbacks
    private void OnPauseTogglePerformed(InputAction.CallbackContext context)
        => SetPaused(!_isPaused);
    
    private void OnIncreasePerformed(InputAction.CallbackContext context)
        => SetTimeScale(timeScale + timeScaleStep);
    
    private void OnDecreasePerformed(InputAction.CallbackContext context)
        => SetTimeScale(timeScale - timeScaleStep);
    #endregion
    
    #region Helpers
    private static void EnableAndSubscribe(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
    {
        if (actionRef.action == null)
            return;
        actionRef.action.Enable();
        actionRef.action.performed += callback;
    }

    private static void DisableAndUnsubscribe(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
    {
        if (actionRef.action == null)
            return;
        actionRef.action.performed -= callback;
        actionRef.action.Disable();
    }
    #endregion
}