using System;

namespace WanderersGuild
{
    public sealed class PanToMenuState : IState
    {
        private readonly MainMenuDirector _director;
        private int _runId; // NOTE: Only the latest pan may advance the machine (guards against stale continuations).

        public PanToMenuState(MainMenuDirector director) => _director = director;

        public void Enter()
        {
            GameEventsRelay.OnTitleScreenDismissed.Invoke();
            _director.TitleScreenView.HidePrompt(); // Runs alongside the pan, so the response feels immediate.
            RunPanAsync(++_runId);
        }

        public void Exit() => _runId++;

        private async void RunPanAsync(int runId)
        {
            try
            {
                await _director.CameraPanner.PanToAsync(_director.MenuAnchor, _director.LifetimeToken);

                if (runId == _runId)
                    _director.EnterMenuState();
            }
            catch (OperationCanceledException)
            {
                // The scene unloaded mid-pan.
            }
        }
    }
}