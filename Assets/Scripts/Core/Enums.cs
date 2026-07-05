// All game enumerations, grouped by system into regions.

#region Guild Rank System
public enum GuildRank
{
    F,
    E,
    D,
    C,
    B,
    A,
    S,
    National
}
#endregion

#region Reputation System
public enum ReputationChangeReason
{
    // Positive
    QuestSuccess,
    FacilityBuilt,
    AdventurerAchievement,
    // Negative
    QuestFailure,
    AdventurerDeath,
    RequestExpired,
    AdventurerDismissed,
    AdventurerDeparted
}

public enum ReputationTier
{
    Reviled,
    Distrusted,
    Unknown,
    Recognized,
    Respected,
    Renowned
}
#endregion

#region Quest System
public enum RequestSource
{
    Kingdom,
    Nobility,
    Merchant,
    Traveler,
    Settlement,
    Organization
}

public enum QuestCategory
{
    Combat,
    Extermination,
    Gathering,
    Escort,
    Delivery,
    Investigation,
    Dungeon
}

public enum QuestDifficulty
{
    Trivial,
    Easy,
    Moderate,
    Hard,
    Severe,
    Extreme,
    Deadly,
    Special
}

public enum QuestState
{
    Draft,
    Posted,
    InProgress,
    Succeeded,
    Failed,
    Expired
}
#endregion

#region Adventurer System
public enum AdventurerClass
{
    Fighter,
    Archer
}

public enum ClassTier
{
    Base,
    Advanced
}

public enum AdventurerState
{
    Idle,
    Applying,
    OnQuest,
    Training,
    Resting,
    Promoting,
    Departing
}

public enum StatType
{
    Strength,
    Dexterity,
    Endurance,
    Wits,
    Spirit
}

public enum DepartureReason
{
    PoorFacilities,
    NoOpportunities,
    LowEarning,
    Retirement,
    Death,
    Dismissed
}

public enum MovementGoal
{
    Idle,
    ToReception,
    ToBoard,
    ToExit,
    Patrol
}
#endregion

#region Party System
public enum PartyState
{
    Forming,
    Idle,
    OnQuest,
    Disbanding
}

public enum RelationshipType
{
    Stranger,
    Acquaintance,
    Friend,
    Close,
    Rival
}
#endregion

#region Facility System
public enum FacilityType
{
    GuildHall,
    Tavern,
    Bedroom,
    Armory,
    Alchemist,
    Office,
    Hallway
}

public enum FacilityState
{
    Locked,
    Available,
    UnderConstruction,
    Operational,
    Upgrading
}

public enum TileEdge
{
    North,
    East,
    South,
    West
}

public enum ConstructionStage
{
    Empty,
    EarlyScaffolding,
    LateScaffolding,
    Finished
}
#endregion

#region Economy System
public enum TransactionType
{
    // Income
    QuestReward,
    AdventurerPurchase,
    GuildService,
    // Expense
    Construction,
    FacilityUpgrade,
    OperationalCost
}
#endregion

#region Equipment System
public enum EquipmentSlot
{
    Weapon,
    OffHand,
    Armor,
    Artifact
}

public enum EquipmentType
{
    Sword,
    Axe,
    Shield,
    Bow,
    Leather,
    HalfPlate
}
#endregion

#region Support System
public enum SupportType
{
    Potion
}
#endregion

#region Time & Simulation System
public enum TimeSpeed
{
    Pause,
    Normal,
    Fast,
    VeryFast
}

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public enum DayPhase
{
    Dawn,
    Morning,
    Midday,
    Afternoon,
    Dusk,
    Evening,
    Night,
    Midnight
}
#endregion

#region UI
public enum ScreenId
{
    None,
    ReceptionDesk,
    QuestBoard
}
#endregion

#region Application
public enum ApplicationStatus
{
    Pending,
    Approved,
    Rejected
}

public enum RegistrationStatus
{
    Pending,
    Registered,
    Rejected
}

public enum ApplicationType
{
    Quest,
    RankUp
}
#endregion