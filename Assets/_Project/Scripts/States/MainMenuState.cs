namespace WanderersGuild
{
    public sealed class MainMenuState : IState
    {
        public void Enter() => GameEventsRelay.OnMainMenuReady.Invoke();

        public void Exit() { }
    }
}