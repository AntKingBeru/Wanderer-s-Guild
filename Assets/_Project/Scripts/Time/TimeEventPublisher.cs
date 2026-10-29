// Raises the Time events after the clock moves: every boundary crossed between the previous and the new time, once each.
namespace WanderersGuild
{
    public static class TimeEventPublisher
    {
        // State on scene start: a tick and the current phase only — never "day started" (that would autosave on load).
        public static void PublishInitial(GameDateTime now)
        {
            GameEventsRelay.OnDayPhaseChanged.Invoke(now.Phase);
            GameEventsRelay.OnTimeTick.Invoke(now);
        }

        // NOTE: Compares fields rather than exact times, so boundaries still fire if a tick ever spans several minutes.
        public static void PublishTick(GameDateTime previous, GameDateTime now)
        {
            var newYear = now.Year != previous.Year;
            var newMonth = newYear || now.Month != previous.Month;
            var newDay = newMonth || now.Day != previous.Day;
            var newHour = newDay || now.Hour != previous.Hour;

            if (newYear)
                GameEventsRelay.OnYearStarted.Invoke(now);
            if (newMonth)
                GameEventsRelay.OnMonthStarted.Invoke(now);
            if (now.Season != previous.Season)
                GameEventsRelay.OnSeasonStarted.Invoke(now.Season);
            if (newDay)
                GameEventsRelay.OnDayStarted.Invoke(now);
            if (newHour)
                GameEventsRelay.OnHourStarted.Invoke(now);
            if (now.Phase != previous.Phase)
                GameEventsRelay.OnDayPhaseChanged.Invoke(now.Phase);

            GameEventsRelay.OnTimeTick.Invoke(now);
        }
    }
}