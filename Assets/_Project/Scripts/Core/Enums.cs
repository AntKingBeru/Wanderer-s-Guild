namespace WanderersGuild
{
    #region Scene Management
    // Logical scene identifiers. Keep mapped to actual scene names in GameConfig.Scenes.
    public enum SceneId
    {
        Bootstrap = 0,
        MainMenu = 1,
        Intro = 2,
        Game = 3
    }
    #endregion
    
    #region Audio
    // Logical music tracks. Mapped to clips in GameConfig.Music.
    public enum MusicTrackId
    {
        None = 0,
        MainMenu = 1,
        Intro = 2
    }
    
    // Mixer channels the player controls in Options. Each maps to one exposed mixer parameter.
    public enum AudioChannel
    {
        Master = 0,
        Music = 1,
        Sfx = 2,
        Character = 3
    }
    #endregion
    
    #region UI
    // Which arrow keys move focus in a MenuNavigation (Tab / Shift+Tab always work).
    public enum NavigationAxis
    {
        Vertical = 0,
        Horizontal = 1
    }
    #endregion
    
    #region MenuMenu
    // Every main-menu button. Each maps to a UXML element and an IMenuCommand.
    public enum MenuAction
    {
        Continue = 0,
        NewGame = 1,
        LoadGame = 2,
        Options = 3,
        Credits = 4,
        QuitGame = 5
    }
    
    // Every panel the main menu can open through MenuPanelHost.
    public enum MenuPanelId
    {
        Credits = 0,
        QuitConfirmation = 1,
        Options = 2,
        LoadGame = 3,
        NewGameSlots = 4,
        Pause = 5,
        RequestBoard = 6,
        Roster = 7,
        Quests = 8,
        ReceptionDesk = 9,
        Applications = 10
    }
    
    // How a ParallaxGroup reacts to the pointer (strength + follow speed set in GameConfig › Parallax).
    public enum ParallaxMotion
    {
        Background = 0,
        Character = 1
    }
    #endregion
    
    #region Settings
    // The Options panel's tabs, in on-screen order.
    public enum SettingsTab
    {
        Audio = 0,
        Display = 1,
        Controls = 2
    }
    
    // Window modes offered in Options. NOTE: The order matches the window-mode cycle labels.
    public enum WindowModeSetting
    {
        Fullscreen = 0,
        Borderless = 1,
        Windowed = 2
    }
    #endregion
    
    #region Guild
    // Guild prestige tiers, lowest first (Systems HLD › Guild Rank).
    public enum GuildRank
    {
        F = 0,
        E = 1,
        D = 2,
        C = 3,
        B = 4,
        A = 5,
        S = 6,
        National = 7
    }

    // Public standing bands; thresholds and colors in GameConfig › Reputation. NOTE: Ordered worst → best.
    public enum ReputationLevel
    {
        Despised = 0,
        Distrusted = 1,
        Neutral = 2,
        Respected = 3,
        Renowned = 4
    }
    
    // What changed reputation (Systems HLD › Reputation sources). For logging and later reactions.
    public enum ReputationSource
    {
        QuestSucceeded = 0,
        FacilityBuilt = 1,
        AdventurerAchievement = 2,
        QuestFailed = 3,
        AdventurerDied = 4,
        RequestExpired = 5,
        AdventurerDismissed = 6,
        AdventurerDeparted = 7,
        RequestDismissed = 8,
        Debug = 99
    }
    
    // Why guild gold moved (Systems HLD › Economy). For logging and later reactions.
    public enum GoldReason
    {
        QuestReward = 0,
        AdventurerPurchase = 1,
        GuildService = 2,
        Construction = 3,
        FacilityUpgrade = 4,
        OperationalCost = 5,
        PromotionFee = 6,
        Debug = 99
    }
    #endregion
    
    #region Save Slots
    // What a slot holds. NOTE: Empty must stay 0 — default SaveSlotInfo values mean "empty".
    public enum SaveSlotState
    {
        Empty = 0,
        Ready = 1,
        Corrupted = 2 // Header present but unreadable: can't be loaded, can be deleted/overwritten.
    }
    
    // What a SaveSlotPanel instance is for.
    public enum SaveSlotPanelMode
    {
        Load = 0,     // Load or delete a guild.
        NewGame = 1   // Pick a slot for a new guild (used slots ask before overwriting).
    }
    #endregion
    
    #region Saving
    // Why a save happened (drives feedback — exit saves are silent).
    public enum SaveReason
    {
        Manual = 0,
        Autosave = 1,
        Exit = 2
    }
    #endregion
    
    #region Time
    // Simulation speed. NOTE: Order matches the HUD speed buttons.
    public enum GameSpeed
    {
        Paused = 0,
        Normal = 1,
        Fast = 2,
        VeryFast = 3
    }

    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }
    
    // Parts of the in-game day; their start hours are set in GameConfig › Time.
    public enum DayPhase
    {
        Dawn = 0,
        Day = 1,
        Dusk = 2,
        Night = 3
    }
    #endregion
    
    #region Pause Menu
    public enum ExitDestination
    {
        MainMenu = 0,
        Desktop = 1
    }
    #endregion
    
    #region Quests
    // What a request or quest is about. NOTE: Shared with the request database tool.
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

    // Who issued a request (Systems HLD › Request Generation).
    public enum RequestSource
    {
        Kingdom,
        Nobility,
        Merchant,
        Traveler,
        Settlement,
        Organization
    }

    // Why a request left the board.
    public enum RequestEndReason
    {
        Accepted = 0,
        Dismissed = 1,
        Expired = 2
    }
    
    // Where a posted quest is. NOTE: Values are saved.
    public enum QuestState
    {
        Waiting = 0,
        InProgress = 1,
        Ready = 2
    }
    
    // How a quest left the guild's list.
    public enum QuestEndReason
    {
        Succeeded = 0,
        Failed = 1,
        Expired = 2,
        Cancelled = 3
    }
    #endregion
    
    #region Adventurers
    // What an adventurer is doing. NOTE: Values are saved; append new ones (OnQuest, Resting…) at the end.
    public enum AdventurerStatus
    {
        Idle = 0,
        SignedUp = 1,
        OnQuest = 2,
        InExam = 3
    }
    #endregion
    
    #region Facilities
    // Where a placed room is. NOTE: Values are saved.
    public enum FacilityState
    {
        Building = 0,
        Complete = 1,
        Upgrading = 2
    }
    
    // Grid facing; order matters — rotating a room adds its rotation steps (N → E → S → W, clockwise from above).
    public enum CardinalDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }
    #endregion
}