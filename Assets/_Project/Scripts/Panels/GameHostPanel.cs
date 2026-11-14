// Game-scene panel host: one panel visible at a time; a panel opened from another returns to it when closed (stack).
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class GamePanelHost : MonoBehaviour, IMenuPanelHost
    {
        [SerializeField] private List<MenuPanel> panels = new();

        private readonly Stack<MenuPanel> _returnTo = new();
        private MenuPanel _active;
        private bool _isBusy;

        public bool IsBusy => _isBusy;
        public bool HasOpenPanel => _active;

        // NOTE: Start runs after every panel's OnEnable has bound its UXML.
        private void Start()
        {
            foreach (var panel in panels.Where(panel => panel))
            {
                panel.Initialize(this);
            }
        }

        public bool HasPanel(MenuPanelId id) => Find(id);
        
        // True while this panel is the open one (its opening and closing included — check IsBusy for "settled").
        public bool IsActive(MenuPanelId id) => _active != null && _active.Id == id;
        
        // A registered panel's panel-root (to find its elements — the tutorial's highlights). Null if none is registered.
        public VisualElement RootOf(MenuPanelId id)
        {
            var panel = Find(id);
            return panel != null ? panel.ScreenRoot : null;
        }

        // Opens a panel; if one is open it closes first and is reopened when the new one closes.
        public void Open(MenuPanelId id)
        {
            if (_isBusy)
                return;

            var panel = Find(id);
            if (panel == null)
            {
                Debug.LogWarning($"[GamePanelHost] No bound panel registered for {id}.", this);
                return;
            }

            if (panel != _active)
                RunOpen(panel);
        }

        // NOTE: The tutorial may lock a panel open until its explanation is done — every close goes through here.
        public void Close(MenuPanelId id)
        {
            if (_isBusy || _active == null || _active.Id != id)
                return;
            if (TutorialManager.HasInstance && !TutorialManager.Instance.CanClose(id))
                return;
            RunClose();
        }

        // NOTE: Only one panel is ever visible — every UIDocument shares one panel root, so two open panels' keyboard
        // navigation would fight each other.
        private async void RunOpen(MenuPanel panel)
        {
            _isBusy = true;
            var token = destroyCancellationToken;

            try
            {
                if (_active != null)
                {
                    await _active.CloseAsync(token);
                    _returnTo.Push(_active);
                }

                _active = panel;
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

        // NOTE: The next panel is chosen before "closed" is raised, so listeners see whether any panel is still open.
        private async void RunClose()
        {
            _isBusy = true;
            var closing = _active;
            var token = destroyCancellationToken;

            try
            {
                await closing.CloseAsync(token);
                _active = _returnTo.Count > 0 ? _returnTo.Pop() : null;
                GameEventsRelay.OnMenuPanelClosed.Invoke(closing.Id);

                if (_active != null)
                {
                    await _active.OpenAsync(token);
                    GameEventsRelay.OnMenuPanelOpened.Invoke(_active.Id);
                }
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
            return panels.FirstOrDefault(panel => panel != null && panel.IsBound && panel.Id == id);
        }
    }
}