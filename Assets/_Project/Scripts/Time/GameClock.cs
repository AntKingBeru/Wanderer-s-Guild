// Holds the current in-game time and advances it; knows nothing about real time or speed.
namespace WanderersGuild
{
    public sealed class GameClock
    {
        private readonly GameCalendar _calendar;

        public GameDateTime Now { get; private set; }

        public GameClock(GameCalendar calendar, long startTotalMinutes)
        {
            _calendar = calendar;
            Now = calendar.FromTotalMinutes(startTotalMinutes);
        }

        // Moves time forward and returns the new time.
        public GameDateTime Advance(int minutes)
        {
            Now = _calendar.FromTotalMinutes(Now.TotalMinutes + minutes);
            return Now;
        }
    }
}