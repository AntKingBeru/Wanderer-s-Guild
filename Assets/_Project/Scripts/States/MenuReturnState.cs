// Main-menu state for returning from a game: the camera already sits on the menu; after the fade-in the menu appears.
namespace WanderersGuild
{
    public sealed class MenuReturnState : IState
    {
        private readonly MainMenuDirector _director;

        public MenuReturnState(MainMenuDirector director) => _director = director;

        public void Enter()
        {
            _director.TitleScreenView.SetPromptOpacityImmediate(0f);

            // NOTE: The menu entrance plays only once the screen is visible.
            if (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy)
                GameEventsRelay.OnSceneTransitionCompleted.AddListener(OnTransitionCompleted);
            else
                _director.EnterMenuState();
        }

        public void Exit() => GameEventsRelay.OnSceneTransitionCompleted.RemoveListener(OnTransitionCompleted);

        private void OnTransitionCompleted(SceneId _)
        {
            GameEventsRelay.OnSceneTransitionCompleted.RemoveListener(OnTransitionCompleted);
            _director.EnterMenuState();
        }
    }
}