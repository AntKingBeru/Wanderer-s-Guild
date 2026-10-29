namespace WanderersGuild
{
    public static class GameSessionLauncher
    {
        public static bool CanLaunch => SceneLoader.HasInstance && !SceneLoader.Instance.IsBusy;

        // New guild in this slot → New Game scene. False if a transition is already running.
        public static bool StartNewGame(int slot)
        {
            if (!CanLaunch || slot < 0)
                return false;

            GameSession.BeginNewGame(slot);
            SceneLoader.Instance.TransitionTo(GameConfig.Instance.SaveSlots.NewGameScene);
            return true;
        }

        // Loads a Ready slot → loaded-game scene, which reads GameSession.ActiveSlot.
        public static bool LoadSlot(int slot)
        {
            if (!CanLaunch || !SaveSlotManager.HasInstance)
                return false;
            if (SaveSlotManager.Instance.GetSlot(slot).State != SaveSlotState.Ready)
                return false;

            GameSession.BeginLoadedGame(slot);
            SceneLoader.Instance.TransitionTo(GameConfig.Instance.SaveSlots.LoadedGameScene);
            return true;
        }
        
        // The Intro is done (guild named and saved) → the game scene. The session keeps IsNewGame = true.
        public static bool EnterGame()
        {
            if (!CanLaunch || !GameSession.HasActiveSlot)
                return false;

            SceneLoader.Instance.TransitionTo(GameConfig.Instance.SaveSlots.LoadedGameScene);
            return true;
        }
    }
}