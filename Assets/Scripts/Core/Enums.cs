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

#region Adventurer
// Governs base stats, category affinities, rank-up quest category and duration, and which future advanced class paths are available.
// Advanced classes (unlocked in training rooms in the build system) will be added here when that system is implemented (DO NOT TOUCH COMMENTED BLOCKS)
public enum AdventurerClass
{
    Fighter, // Melee specialist. High Strength (used for hidden tag check). Preferred: Combat, Subjugation, DungeonDelving.
    Archer, // Ranged specialist. High Dexterity (used for hidden tag check). Preferred: Gathering, Exploration, Investigation, Subjugation.
    // Future advanced classes (unlocked in training)
    // Barbarian, // Fighter → Barbarian path
    // Paladin, // Fighter → Paladin path (requires Priest)
    // Ranger, // Archer → Ranger path
}

// How a quest category relates to a specific adventurer class.
// Applied per party member when calculation total success chance in AdventurerManager
public enum CategoryAffinity
{
    Preferred, // Positive modifier
    Neutral, // No modifier
    Disliked // Negative modifier
}

// Primary engagement status of an adventurer.
// Rank-up quest application are tracked in a separate field on AdventurerData so they do not conflict with regular quest state.
// An adventurer can have a pending rank-up application while simultaneously being Idle, AppliedToQuest, or OnQuest, but cannot be dispatched to both at once.
public enum AdventurerStatus
{
    Idle, // Free to browse posted quests and submit applications
    AppliedToQuest, // Has a pending regular quest application awaiting player approval
    OnQuest, // Dispatched on a regular quest; unavailable for other quests
    OnRankUpQuest, // Dispatched on a rank-up quest; regular quest dispatch blocked
    // Placeholders (injury and death system not yet implemented)
    // Set these values in code only when the corresponding system is built.
    Injured, // Temporarily unable to take quests; recovers over time
    Dead // Permanently removed from the active roster
}

// Where an adventurer sleeps each night.
// Checked at midnight by AdventurerManager.
// InGuild requires an available bed in a housing room (build system adds this).
// Nowhere causes sleep maintenance to degrade each night until a bed is assigned.
public enum LodgingState
{
    InGuild, // Assigned to a bed in a guild housing room
    OutsideGuild, // Has private accommodation; no guild cost; uncommon
    Nowhere // Unhoused; sleep penalty accumulates nightly
}

// The event that causes a change to a party's composition or status.
// Stored on the change event so AdventurerManager and future UI can display history.
public enum PartyChangeReason
{
    Formed, // Initial party creation (permanent or temporary)
    MemberJoined, // An adventurer was added to an existing party
    MemberLeft, // An adventurer voluntarily departed the party
    RankDifference, // Members split off due to a rank gap exceeding the threshold
    MemberDied, // One or more party members died during a quest
    ConsecutiveFailures, // Too many consecutive failures caused dissolution
    LowMorale, // Multiple members returned with critically low HP
    Disbanded, // The party was fully dissolved; all members become solo
    TemporaryMadePermanent // A temporary per-quest grouping became a registered party
}

// All possible movement/behavior states an in-world adventurer object can be in.
// AdventurerNavigationController transitions between these in response to game events from GameEventRelay.
public enum AdventurerBehaviorState
{
    Idle, // Wandering the designated patrol area
    Arriving, // Walking to a reception desk prop on first spawn
    Browsing, // Walking to a guild board prop to browse quests
    Departing, // Walking to the exit/spawn point before going on a quest
    OnQuest, // Hidden from the world; quest is in progress
    Returning // Walking from the exit/spawn point back to idle area
}
#endregion

#region Build
public enum RoomState
{
    UnderConstruction,
    Built
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

public enum GuildPointType
{
    ReceptionDesk, // Where adventurers walk on first arrival
    QuestBoard, // Where adventurers walk when browsing for quests
    Exit // Where adventurers walk before departing on a quest / after returning
}
#endregion

#region Reputation
public enum ReputationLevel
{
    ExtremelyLow = -51,
    Low  = -1,
    Average = 50,
    High
}
#endregion