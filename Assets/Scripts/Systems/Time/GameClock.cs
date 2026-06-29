// Pure calendar logic: tracks date + time-of-day and rolls over days/seasons/years.

using System;

public class GameClock
{
    private readonly int _daysPerSeason;
    private double _timeOfDay;

    public GameDate CurrentDate { get; private set; }
    
    // Normalized 0..1 fraction of the current day (clamped for safety before draining).
    public float TimeOfDay => (float)Math.Min(_timeOfDay, 1.0);
    public DayPhase CurrentPhase => PhaseFromFraction(TimeOfDay);

    public GameClock(GameDate start, int daysPerSeason)
    {
        CurrentDate = start;
        _daysPerSeason = Math.Max(1, daysPerSeason);
    }
    
    public void AddTime(double deltaDays) => _timeOfDay += deltaDays;

    public bool TryConsumeDay(out bool seasonChanged, out bool yearChanged)
    {
        seasonChanged = false;
        yearChanged = false;
        if (_timeOfDay < 1.0)
            return false;
        
        _timeOfDay -= 1.0;
        AdvanceOneDay(out seasonChanged, out yearChanged);
        return true;
    }

    private void AdvanceOneDay(out bool seasonChanged, out bool yearChanged)
    {
        seasonChanged = false;
        yearChanged = false;

        var day = CurrentDate.day + 1;
        var season = CurrentDate.season;
        var year = CurrentDate.year;

        if (day > _daysPerSeason)
        {
            day = 1;
            seasonChanged = true;
            if (season == Season.Winter)
            {
                season = Season.Spring;
                year++;
                yearChanged = true;
            }
            else
                season = (Season)((int)season + 1);
        }
        
        CurrentDate = new GameDate(year, season, day);
    }
    
    private static DayPhase PhaseFromFraction(double t) => t switch
    {
        < 0.2083 => DayPhase.Midnight,    // 00:00–05:00
        < 0.2917 => DayPhase.Dawn,        // 05:00–07:00
        < 0.4583 => DayPhase.Morning,     // 07:00–11:00
        < 0.5417 => DayPhase.Midday,      // 11:00–13:00
        < 0.7083 => DayPhase.Afternoon,   // 13:00–17:00
        < 0.7917 => DayPhase.Dusk,        // 17:00–19:00
        < 0.9167 => DayPhase.Evening,     // 19:00–22:00
        _  => DayPhase.Night              // 22:00–24:00
    };
}