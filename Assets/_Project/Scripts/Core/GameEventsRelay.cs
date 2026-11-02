using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace WanderersGuild
{
    public static class GameEventsRelay
    {
        #region Scene Management
        // An async scene load began. Arg: target scene.
        public static readonly UnityEvent<SceneId> OnSceneLoadStarted = new();

        // Raised every frame while loading. Arg: normalized progress 0-1.
        public static readonly UnityEvent<float> OnSceneLoadProgress = new();
        
        // The scene is activated (before the fade-in). Arg: loaded scene.
        public static readonly UnityEvent<SceneId> OnSceneLoadCompleted = new();
        
        // The fade-in finished and the loader is idle; the scene is visible and interactive. Arg: scene.
        public static readonly UnityEvent<SceneId> OnSceneTransitionCompleted = new();
        #endregion
        
        #region Bootstrap
        // A boot step is starting. Arg: step name.
        public static readonly UnityEvent<string> OnBootstrapStepStarted = new();
        
        // All boot steps finished; the hand-off to the first scene is next.
        public static readonly UnityEvent OnBootstrapCompleted = new();
        #endregion
        
        #region UI
        // Any pixel button became selected (hover or keyboard). Hook for hover SFX.
        public static readonly UnityEvent OnUIButtonFocused = new();
        
        // Any pixel button was clicked or submitted. Hook for click SFX.
        public static readonly UnityEvent OnUIButtonClicked = new();
        #endregion
        
        #region Main Menu
        // The player pressed any key on the title screen; the camera pan begins.
        public static readonly UnityEvent OnTitleScreenDismissed = new();
        
        // The camera reached the menu background; the menu UI may appear.
        public static readonly UnityEvent OnMainMenuReady = new();
        
        // A menu button became selected (hover or keyboard). Arg: its action. Hook for hover SFX.
        public static readonly UnityEvent<MenuAction> OnMenuButtonFocused = new();
        
        // A menu action was activated. Arg: the action. Hook for click SFX.
        public static readonly UnityEvent<MenuAction> OnMenuActionInvoked = new();
        
        // A menu panel finished opening. Arg: the panel. Hook for panel SFX.
        public static readonly UnityEvent<MenuPanelId> OnMenuPanelOpened = new();
        
        // A menu panel finished closing (menu restored). Arg: the panel.
        public static readonly UnityEvent<MenuPanelId> OnMenuPanelClosed = new();
        #endregion
        
        #region Settings
        // Settings were loaded or restored to defaults in bulk; open views should refresh their values.
        public static readonly UnityEvent OnSettingsApplied = new();
        #endregion
        
        #region Internal
        // NOTE: With domain reload disabled, static events keep stale listeners between play sessions.
        // This clears every UnityEvent declared above automatically, including future ones.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAllListeners()
        {
            var fields = typeof(GameEventsRelay).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
                if (field.GetValue(null) is UnityEventBase unityEvent)
                    unityEvent.RemoveAllListeners();
        }
        #endregion
        
        #region Save Slots
        // The save slots were re-read (open, delete, scene load); slot lists and menu availability should refresh.
        public static readonly UnityEvent OnSaveSlotsChanged = new();
        #endregion
        
        #region Saving
        // A game save began (not raised for exit saves' feedback purposes — check the reason). Arg: why.
        public static readonly UnityEvent<SaveReason> OnGameSaveStarted = new();
        
        // A game save finished. Args: why, and whether every file was written.
        public static readonly UnityEvent<SaveReason, bool> OnGameSaveCompleted = new();
        #endregion
        
        #region Intro
        // The intro cutscene ended (played through or skipped). Arg: true when skipped.
        public static readonly UnityEvent<bool> OnIntroCutsceneFinished = new();
        #endregion
        
        #region Time
        // One simulation tick passed (one in-game minute). Arg: the new time.
        public static readonly UnityEvent<GameDateTime> OnTimeTick = new();

        // A new in-game hour began. Arg: the new time.
        public static readonly UnityEvent<GameDateTime> OnHourStarted = new();

        // A new in-game day began (00:00). Not raised when a game loads. Hook for the daily autosave.
        public static readonly UnityEvent<GameDateTime> OnDayStarted = new();

        // A new month began. Arg: the new time.
        public static readonly UnityEvent<GameDateTime> OnMonthStarted = new();

        // A new season began. Arg: the season.
        public static readonly UnityEvent<Season> OnSeasonStarted = new();

        // A new year began. Arg: the new time.
        public static readonly UnityEvent<GameDateTime> OnYearStarted = new();

        // The day phase changed (also raised once when the game scene starts). Arg: the phase.
        public static readonly UnityEvent<DayPhase> OnDayPhaseChanged = new();

        // The simulation speed changed (Paused included). Arg: the speed.
        public static readonly UnityEvent<GameSpeed> OnGameSpeedChanged = new();
        #endregion
        
        #region Guild
        // Guild gold changed. Args: new total, applied change (0 = initial value on scene start).
        public static readonly UnityEvent<long, long> OnGoldChanged = new();

        // Reputation changed. Args: new value, applied change (0 = initial value on scene start).
        public static readonly UnityEvent<int, int> OnReputationChanged = new();

        // The reputation level changed (also raised once on scene start). Arg: the level.
        public static readonly UnityEvent<ReputationLevel> OnReputationLevelChanged = new();

        // Rank points changed. Args: current rank, progress toward the next rank (0–1; 1 at the highest rank).
        public static readonly UnityEvent<GuildRank, float> OnRankProgressChanged = new();

        // The guild reached a new rank (once per rank, in order). Not raised on load. Arg: the new rank.
        public static readonly UnityEvent<GuildRank> OnGuildRankUp = new();
        #endregion
        
        #region Quests
        // A request was posted to the board (dawn batch or starting batch). Arg: the request.
        public static readonly UnityEvent<ActiveRequest> OnRequestAdded = new();

        // A request left the board. Args: the request, and why (accepted, dismissed, expired).
        public static readonly UnityEvent<ActiveRequest, RequestEndReason> OnRequestRemoved = new();

        // A request was turned into a posted quest. Arg: the quest.
        public static readonly UnityEvent<Quest> OnQuestPosted = new();

        // A quest changed: an adventurer signed up, or the party departed. Arg: the quest.
        public static readonly UnityEvent<Quest> OnQuestUpdated = new();

        // A quest left the list. Args: the quest, and why (succeeded, failed, expired, canceled).
        public static readonly UnityEvent<Quest, QuestEndReason> OnQuestEnded = new();
        
        // A quest's party filled and is waiting for the player's approval. Arg: the quest.
        public static readonly UnityEvent<Quest> OnQuestPartyReady = new();
        #endregion
        
        #region Advnenturers
        // An adventurer joined the guild (arrival or starting adventurer). Arg: the adventurer.
        public static readonly UnityEvent<Adventurer> OnAdventurerJoined = new();

        // An adventurer reached a new level (once per level gained). Arg: the adventurer (Level is the new level).
        public static readonly UnityEvent<Adventurer> OnAdventurerLeveledUp = new();
        
        // An adventurer's status or wealth changed (sign-up, departure, return, reward). Arg: the adventurer.
        public static readonly UnityEvent<Adventurer> OnAdventurerChanged = new();
        
        // An adventurer left for a promotion exam. Args: the adventurer, the rank they're testing for.
        public static readonly UnityEvent<Adventurer, GuildRank> OnPromotionExamStarted = new();

        // A promotion exam ended. Args: the adventurer, the rank tested for, whether they passed.
        public static readonly UnityEvent<Adventurer, GuildRank, bool> OnPromotionExamEnded = new();
        
        // An adventurer left the guild (dismissed, died, departed — raised by those systems when they exist).
        // Arg: the adventurer.
        public static readonly UnityEvent<Adventurer> OnAdventurerLeft = new();
        #endregion
        
        #region Facilities
        // A room was placed (construction started; the Guild Hall arrives already complete). Arg: the room.
        public static readonly UnityEvent<PlacedFacility> OnFacilityPlaced = new();

        // A construction or upgrade finished — the room now works at its new level. Arg: the room.
        public static readonly UnityEvent<PlacedFacility> OnFacilityCompleted = new();

        // An upgrade started. Arg: the room.
        public static readonly UnityEvent<PlacedFacility> OnFacilityUpgradeStarted = new();

        // A room was demolished. Arg: the room (no longer in the guild).
        public static readonly UnityEvent<PlacedFacility> OnFacilityRemoved = new();
        
        // Build mode opened (true) or closed (false).
        public static readonly UnityEvent<bool> OnBuildModeChanged = new();
        #endregion
    }
}