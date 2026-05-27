// Central repository for all game-wide enumerations.
// Add new enums here as each system is built rather than scattering them across files and them getting lost.
// When adding a new enum, specify what it should do and what it will use it

// Broad categorization of the time of day, used by quest availability and adventurer behavior systems.
public enum TimeOfDay
{
    Midnight, // 00:00 - 04:59
    Dawn, // 05:00 - 06:59
    Morning, // 07:00 - 11:59
    Noon, // 12:00 - 12:59
    Afternoon, // 13:00 - 16:59
    Evening, // 17:00 - 18:59
    Dusk, // 19:00 - 20:59
    Night // 21:00 - 23:59
}

// Seasonal cycle driven by month progression in TimeManager.
// Affects adventurer moral, quest types, and future weather systems (if we decide to add them).
public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

// Named days of the weak for scheduling guild events and rest days.
// Named "Weekday" (not DayOfWeek) to avoid ambiguity with System.DayOfWeak in files that import System.
public enum Weekday
{
    Moonday,
    Tysday,
    Odinday,
    Thorday,
    Frigday,
    Laufeyday,
    Sunday
}