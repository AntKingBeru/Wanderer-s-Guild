// New Game: transitions to the Intro scene, which will hold the premise cutscene and the guild-name prompt.
using UnityEngine;

namespace WanderersGuild
{
    public sealed class NewGameCommand : IMenuCommand
    {
        private readonly IMenuPanelHost _host;

        public NewGameCommand(IMenuPanelHost host) => _host = host;

        // NOTE: Greyed out if the SaveSlotManager prefab is missing — a new game would have nowhere to save.
        public bool IsAvailable => SaveSlotManager.HasInstance;

        public bool CanExecute => GameSessionLauncher.CanLaunch && (_host == null || (!_host.IsBusy && !_host.HasOpenPanel));

        public void Execute()
        {
            if (SaveSlotManager.Instance.TryGetFirstEmpty(out var slot))
            {
                GameSessionLauncher.StartNewGame(slot);
                return;
            }

            if (_host != null && _host.HasPanel(MenuPanelId.NewGameSlots))
                _host.Open(MenuPanelId.NewGameSlots);
            else
                Debug.LogWarning("[NewGameCommand] Every slot is used and no NewGameSlots panel is registered.");
        }
    }
}