// Converts between total in-game minutes and calendar date/time (year, month, day, hour, minute, season, phase).
using System;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class GameCalendar
    {
        public const int MinutesPerHour = 60;
        public const int HoursPerDay = 24;
        public const int MinutesPerDay = MinutesPerHour * HoursPerDay;

        // NOTE: Used when the configured phase hours aren't ascending.
        private const int DefaultDawn = 5, DefaultDay = 7, DefaultDusk = 18, DefaultNight = 20;

        private readonly TimeSettings _settings;
        private readonly int _dawn, _day, _dusk, _night;

        public GameCalendar(TimeSettings settings)
        {
            _settings = settings;

            if (settings.HasValidPhaseHours)
            {
                (_dawn, _day, _dusk, _night) = (settings.DawnHour, settings.DayHour, settings.DuskHour, settings.NightHour);
            }
            else
            {
                Debug.LogWarning("[GameCalendar] Day-phase hours must be ascending (Dawn < Day < Dusk < Night); using defaults.");
                (_dawn, _day, _dusk, _night) = (DefaultDawn, DefaultDay, DefaultDusk, DefaultNight);
            }
        }

        public GameDateTime FromTotalMinutes(long totalMinutes)
        {
            var total = Math.Max(0L, totalMinutes);
            var totalDays = total / MinutesPerDay;
            var minuteOfDay = (int)(total % MinutesPerDay);

            var dayOfYear = (int)(totalDays % _settings.DaysPerYear);
            var year = (int)(totalDays / _settings.DaysPerYear) + 1;
            var month = dayOfYear / _settings.DaysPerMonth + 1;
            var day = dayOfYear % _settings.DaysPerMonth + 1;
            var hour = minuteOfDay / MinutesPerHour;
            var minute = minuteOfDay % MinutesPerHour;

            return new GameDateTime(total, year, month, day, hour, minute, _settings.SeasonOf(month), PhaseAt(hour));
        }

        // NOTE: Clamps everything into the calendar, so dates from older or hand-edited saves can't break the clock.
        public long ToTotalMinutes(GameDateStamp date, int hour)
        {
            long year = Math.Max(1, date.Year);
            var month = Mathf.Clamp(date.Month, 1, _settings.MonthsPerYear);
            var day = Mathf.Clamp(date.Day, 1, _settings.DaysPerMonth);
            var clampedHour = Mathf.Clamp(hour, 0, HoursPerDay - 1);

            var totalDays = (year - 1) * _settings.DaysPerYear + (month - 1) * _settings.DaysPerMonth + (day - 1);
            return totalDays * MinutesPerDay + clampedHour * MinutesPerHour;
        }

        // NOTE: Night wraps past midnight (e.g. 20:00 → 04:59).
        public DayPhase PhaseAt(int hour)
        {
            if (hour >= _night || hour < _dawn)
                return DayPhase.Night;
            if (hour >= _dusk)
                return DayPhase.Dusk;
            if (hour >= _day)
                return DayPhase.Day;
            return DayPhase.Dawn;
        }
    }
}