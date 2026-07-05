// Singleton driver: ticks the GameClock by the active speed multiplier and broadcasts time events.

using UnityEngine;

[DefaultExecutionOrder(-90)]
public class TimeController : MonoSingleton<TimeController>
{
    private GameClock _clock;
    private TimeSpeedStateMachine _speed;
    
    public int Hour => _clock.Hour;
    public int Minute => _clock.Minute;
    public GameDate CurrentDate => _clock.CurrentDate;
    public float TimeOfDay => _clock.TimeOfDay;
    public DayPhase CurrentPhase => _clock.CurrentPhase;
    public TimeSpeed CurrentSpeed => _speed.Current.Speed;
    public float CurrentSpeedMultiplier => _speed.Current.Multiplier;

    protected override void OnSingletonAwake()
    {
        var config = GameConfig.Instance.Time;
        var startFraction = Mathf.Clamp(config.startHour, 0, 23) / 24.0;
        _clock = new GameClock(new GameDate(1, Season.Spring, 1), config.daysPerSeason, startFraction);
        _speed = new TimeSpeedStateMachine(config.fastMultiplier, config.veryFastMultiplier);
    }

    private void Update()
    {
        var multiplier = _speed.Current.Multiplier;
        if (multiplier <= 0f)
            return;

        var daysPerSecond = 1.0 / Mathf.Max(0.0001f, GameConfig.Instance.Time.realSecondsPerDay);
        _clock.AddTime(Time.deltaTime * multiplier * daysPerSecond);
        
        var relay = GameEventsRelay.Instance;
        
        while (_clock.TryConsumeDay(out var seasonChanged, out _))
        {
            relay.RaiseDayAdvanced(_clock.CurrentDate);
            if (seasonChanged)
                relay.RaiseSeasonChanged(_clock.CurrentDate.season);
        }
        
        while (_clock.TryConsumeHour(out var hour))
            relay.RaiseHourAdvanced(hour);
    }

    public void SetSpeed(TimeSpeed speed)
    {
        if (_speed.SetSpeed(speed))
            GameEventsRelay.Instance.RaiseTimeSpeedChanged(CurrentSpeed);
    }

    public void TogglePause()
    {
        if (_speed.TogglePause())
            GameEventsRelay.Instance.RaiseTimeSpeedChanged(CurrentSpeed);
    }
    
    public void CycleSpeed()
    {
        if (_speed.CycleSpeed())
            GameEventsRelay.Instance.RaiseTimeSpeedChanged(CurrentSpeed);
    }
}