// Opens the in-game pause menu (Esc or the HUD button) and keeps time paused until every game panel has closed.
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GamePanelHost host;

        private ScopedActionListener _listener;
        private bool _isMenuOpen;

        private void OnEnable()
        {
            _listener = new ScopedActionListener(GameConfig.Instance.InputBindings.PauseMenu, OnPauseMenuPressed);
            GameEventsRelay.OnMenuPanelClosed.AddListener(OnPanelClosed);
        }

        private void OnDisable()
        {
            _listener?.Dispose();
            _listener = null;
            GameEventsRelay.OnMenuPanelClosed.RemoveListener(OnPanelClosed);
        }

        // Opens the pause menu if nothing is in the way (host busy, a panel open, time not running, scene
        // loading). True if it opened. (Build mode closes itself when the menu opens.)
        public bool TryOpen()
        {
            if (!host || host.IsBusy || host.HasOpenPanel)
                return false;
            if (!TimeManager.HasInstance || !TimeManager.Instance.IsRunning)
                return false;
            if (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy)
                return false;

            TimeManager.Instance.EnterMenuPause();
            _isMenuOpen = true;
            host.Open(MenuPanelId.Pause);
            return true;
        }

        // NOTE: Only ever opens — Esc inside the menu is UI Cancel. Esc belongs to build mode while it's open, or if build
        // mode used it this frame (the two listeners can fire in either order).
        private void OnPauseMenuPressed(InputAction.CallbackContext _)
        {
            if (BuildModeController.HasInstance && BuildModeController.Instance.BlocksEscape)
                return;
            TryOpen();
        }

        // NOTE: Options → pause menu keeps a panel open, so time only resumes once nothing is open.
        private void OnPanelClosed(MenuPanelId _)
        {
            if (!_isMenuOpen || host.HasOpenPanel)
                return;

            _isMenuOpen = false;
            if (TimeManager.HasInstance)
                TimeManager.Instance.ExitMenuPause();
        }
    }
}