// Opens the brewing screen for the morning prompt once nothing is in the way, and keeps time paused while it's open.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: On GamePanelHost. Only the pause it took is released — the pause menu manages its own.
    public class AlchemyPromptController : MonoBehaviour
    {
        [SerializeField] private GamePanelHost host;

        private bool _pending;
        private bool _ownsPause;

        private void OnEnable()
        {
            GameEventsRelay.OnBrewPromptRequested.AddListener(OnPromptRequested);
            GameEventsRelay.OnMenuPanelClosed.AddListener(OnPanelClosed);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnBrewPromptRequested.RemoveListener(OnPromptRequested);
            GameEventsRelay.OnMenuPanelClosed.RemoveListener(OnPanelClosed);
            ReleasePause();
        }

        private void OnPromptRequested(int _) => _pending = true;

        // NOTE: Waits for a clear moment (no panel, host idle, no scene load); a prompt whose slots filled meanwhile is dropped.
        private void LateUpdate()
        {
            if (!_pending || host is null || host.IsBusy || host.HasOpenPanel)
                return;
            if (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy)
                return;

            _pending = false;
            if (!AlchemyManager.HasInstance || AlchemyManager.Instance.FreeSlots <= 0)
                return;

            if (TimeManager.HasInstance && TimeManager.Instance.IsRunning)
            {
                TimeManager.Instance.EnterMenuPause();
                _ownsPause = true;
            }

            AlchemyPanel.PrepareBrewing();
            host.Open(MenuPanelId.Alchemy);
        }

        private void OnPanelClosed(MenuPanelId id)
        {
            if (id == MenuPanelId.Alchemy)
                ReleasePause();
        }

        private void ReleasePause()
        {
            if (!_ownsPause)
                return;

            _ownsPause = false;
            if (TimeManager.HasInstance)
                TimeManager.Instance.ExitMenuPause();
        }
    }
}