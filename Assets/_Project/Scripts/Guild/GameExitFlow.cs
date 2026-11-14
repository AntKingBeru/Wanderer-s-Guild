// Leaving a running game: saves first (with a thumbnail), then returns to the main menu or quits to the desktop.
namespace WanderersGuild
{
    public sealed class GameExitFlow
    {
        // True when saved — or when there's nothing to save (no active slot, or saving held back by the start tutorial: the
        // slot keeps the new guild's first save, so quitting mid-tutorial just leaves).
        public async UnityEngine.Awaitable<bool> TrySaveAsync()
        {
            if (!SaveGameService.HasInstance || !GameSession.HasActiveSlot || SaveBlockers.IsBlocked)
                return true;
            return await SaveGameService.Instance.SaveAsync(SaveReason.Exit);
        }

        // NOTE: The session ends first, so SaveGameService.OnApplicationQuit finds nothing left to save.
        public void Leave(ExitDestination destination)
        {
            GameSession.End();

            if (destination == ExitDestination.MainMenu)
            {
                MainMenuEntry.RequestSkipTitle();
                if (SceneLoader.HasInstance)
                    SceneLoader.Instance.TransitionTo(SceneId.MainMenu);
                return;
            }

            IMenuCommand quit = new QuitGameCommand();
            if (quit.CanExecute)
                quit.Execute();
        }
    }
}