// Pure calendar/clock state: holds total game-minutes and derives date + time-of-day components.
namespace WanderersGuild
{
    public class GameClock
    {
        private readonly int _minutesPerHour;
        private readonly int _hoursPerDay;
        private readonly int _daysPerMonth;
        private readonly int _monthsPerYear;

        private readonly int _minutesPerDay;
        private readonly int _minutesPerMonth;
        private readonly int _minutesPerYear;
        
        // Canonical time since start (00:00, Day 1, Month 1, Year 1). Everything derives from this.
        public long TotalMinutes { get; private set; }

        public GameClock(int minutesPerHour, int hoursPerDay, int daysPerMonth, int monthsPerYear, long startMinutes = 0)
        {
            _minutesPerHour = minutesPerHour;
            _hoursPerDay = hoursPerDay;
            _daysPerMonth = daysPerMonth;
            _monthsPerYear = monthsPerYear;

            _minutesPerDay = minutesPerHour * hoursPerDay;
            _minutesPerMonth = _minutesPerDay * daysPerMonth;
            _minutesPerYear = _minutesPerMonth * monthsPerYear;

            TotalMinutes = startMinutes;
        }

        public int Minute => (int)(TotalMinutes % _minutesPerHour);
        public int Hour => (int)(TotalMinutes / _minutesPerHour % _hoursPerDay);
        public int Day => (int)(TotalMinutes / _minutesPerDay   % _daysPerMonth) + 1;
        public int Month => (int)(TotalMinutes / _minutesPerMonth % _monthsPerYear) + 1;
        public int Year => (int)(TotalMinutes / _minutesPerYear) + 1;
        public Season Season => (Season)SeasonIndex();

        public GameDate CurrentDate => new(Day, Month, Year, Season);

        // Advances time. Rollover detection is the caller's job (compare dates before/after).
        public void AdvanceMinutes(int minutes) => TotalMinutes += minutes;

        // Restore exact time on load.
        public void SetTotalMinutes(long minutes) => TotalMinutes = minutes;

        // Maps the current month evenly onto 4 seasons — works for any monthsPerYear, not just 12.
        private int SeasonIndex()
        {
            var idx = (Month - 1) * 4 / _monthsPerYear;
            return idx < 0 ? 0 : idx > 3 ? 3 : idx;
        }

        // "HH:MM" for UI.
        public string TimeOfDayString => $"{Hour:00}:{Minute:00}";
    }
}