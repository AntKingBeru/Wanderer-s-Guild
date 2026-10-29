using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public class MenuPanelHost : MonoBehaviour, IMenuPanelHost
    {
        [SerializeField] private MainMenuPresenter presenter;
        [SerializeField] private MenuEntranceAnimator menuAnimator;
        [SerializeField] private List<MenuPanel> panels = new();

        private MenuPanel _active;
        private bool _isBusy;

        public bool IsBusy => _isBusy;
        public bool HasOpenPanel => _active;

        // NOTE: Start runs after every panel's OnEnable has bound its UXML.
        private void Start()
        {
            if (!presenter || !menuAnimator)
                Debug.LogError("[MenuPanelHost] Missing presenter or menu animator.", this);

            foreach (var panel in panels.Where(panel => panel))
            {
                panel.Initialize(this);
            }
        }

        public bool HasPanel(MenuPanelId id) => Find(id);

        public void Open(MenuPanelId id)
        {
            if (_isBusy || _active)
                return;

            var panel = Find(id);
            if (!panel)
            {
                Debug.LogWarning($"[MenuPanelHost] No bound panel registered for {id}.", this);
                return;
            }

            RunOpen(panel);
        }

        public void Close(MenuPanelId id)
        {
            if (_isBusy || !_active || _active.Id != id)
                return;
            RunClose(_active);
        }

        private async void RunOpen(MenuPanel panel)
        {
            _isBusy = true;
            _active = panel;
            presenter.SetInteractive(false);
            var token = destroyCancellationToken;

            try
            {
                if (panel.HidesMenu)
                    await menuAnimator.HideAsync(token);

                await panel.OpenAsync(token);
                GameEventsRelay.OnMenuPanelOpened.Invoke(panel.Id);
            }
            catch (OperationCanceledException)
            {
                // Scene unloaded mid-transition.
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void RunClose(MenuPanel panel)
        {
            _isBusy = true;
            var token = destroyCancellationToken;

            try
            {
                await panel.CloseAsync(token);
                _active = null;

                if (panel.HidesMenu)
                    await menuAnimator.PlayAsync(token, GameConfig.Instance.PanelTransitions.MenuReturnSpeed);

                presenter.SetInteractive(true);
                presenter.RestoreFocus();
                GameEventsRelay.OnMenuPanelClosed.Invoke(panel.Id);
            }
            catch (OperationCanceledException)
            {
                // Scene unloaded mid-transition.
            }
            finally
            {
                _isBusy = false;
            }
        }

        private MenuPanel Find(MenuPanelId id)
        {
            return panels.FirstOrDefault(panel => panel && panel.IsBound && panel.Id == id);
        }
    }
}