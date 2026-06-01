// Central repository for all game-wide enumerations.
// Add new enums here as each system is built rather than scattering them across files and them getting lost.
// When adding a new enum, specify what it should do and what it will use it
#region Time
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
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
}
#endregion

#region Quest
// The type of work a quest involves. Determines class affinity modifiers and the pool of applicable adventurer classes.
// Affinity data is defined per-class in the adventurer system and looked up against this value at success-chance time.
public enum QuestCategory
{
    Combat,
    Gathering,
    Escort,
    Delivery,
    Subjugation,
    Exploration,
    Investigation,
    Dungeon
}

// Quest difficulty rank. The integer value is used directly in formulas.
// Display name and color come from QuestConfig so designers can change them without touching code.
public enum QuestRank
{
    F = 0,
    E = 1,
    D = 2,
    C = 3,
    B = 4,
    A = 5,
    S = 6,
    Special = 7
}

// Full lifecycle of a quest from the moment it is created to its final resolution.
public enum QuestStatus
{
    Unposted, // Created at the reception desk; sitting in the unposted list
    Posted, // On the board; time limit countdown is active
    InProgress, // An application was approved; party is on the quest
    Completed, // Party returned successfully within the time limit
    Failed, // Party failed, or the time limit expired while in progress
    Expired // Time limit elapsed before any application was approved
}

// Lifecycle of a party's application for a specific posted quest.
public enum ApplicationStatus
{
    Pending, // Submitted by the party; awaiting guild manager review
    Approved, // Guild manager approved; party transitions to InProgress
    Rejected // Guild manager declined the application
}
#endregion

#region World Interaction
// Identifies which UI screen a world-space interactable prop opens when clicked.
// Add entries here as new screen are introduced.
public enum ScreenType
{
    ReceptionDesk,
    QuestBoard
}
#endregion

#region Reputation

public enum ReputationLevel
{
    ExtremelyLow,
    Low,
    Average,
    High
}

#endregion