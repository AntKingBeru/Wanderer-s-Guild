using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField] private MainMenuView view;
        [SerializeField] private MenuEntranceAnimator entranceAnimator;
        [SerializeField] private MenuPanelHost panelHost;

        private readonly Dictionary<MenuAction, IMenuCommand> _commands = new();
        private MenuNavigation _navigation;
        private VisualElement _navigationHost;
        private Button _lastFocused;
        private bool _isInteractive;
        private bool _isShowing;

        private void Awake() => RegisterCommands();

        private void Start()
        {
            if (!view || !view.IsBound || !entranceAnimator)
            {
                Debug.LogError("[MainMenuPresenter] Missing or unbound view/animator.", this);
                enabled = false;
                return;
            }

            WireButtons();
            AttachNavigation();
            SetInteractive(false);
            entranceAnimator.PrepareHidden();

            GameEventsRelay.OnMainMenuReady.AddListener(OnMainMenuReady);
            GameEventsRelay.OnSceneLoadStarted.AddListener(OnSceneLoadStarted);
            GameEventsRelay.OnSaveSlotsChanged.AddListener(RefreshAvailability);
        }

        private void OnDestroy()
        {
            GameEventsRelay.OnMainMenuReady.RemoveListener(OnMainMenuReady);
            GameEventsRelay.OnSceneLoadStarted.RemoveListener(OnSceneLoadStarted);
            GameEventsRelay.OnSaveSlotsChanged.RemoveListener(RefreshAvailability);

            // NOTE: The panel belongs to the PanelSettings asset and can outlive this scene — never leave callbacks on it.
            if (_navigationHost != null && _navigation != null)
                _navigationHost.RemoveManipulator(_navigation);
        }

        // Enables or blocks the menu column. Disabling also clears the selected button's highlight.
        public void SetInteractive(bool interactive)
        {
            _isInteractive = interactive;
            if (!interactive) BlurIfFocusedInMenu();
            view.SetButtonsInteractable(interactive);
        }

        // Re-selects the button that was selected before a panel opened (or the first available one).
        public void RestoreFocus()
        {
            if (_lastFocused is { enabledInHierarchy: true, focusable: true })
                _lastFocused.Focus();
            else
                FocusFirstAvailable();
        }

        // NOTE: Commands are the only place menu behavior lives (Command pattern).
        private void RegisterCommands()
        {
            // NOTE: Converts Unity's "fake null" (unassigned field) into a real null before it reaches an interface.
            IMenuPanelHost host = panelHost ? panelHost : null;

            _commands[MenuAction.Continue] = new ContinueCommand();
            _commands[MenuAction.NewGame] = new NewGameCommand(host);
            _commands[MenuAction.LoadGame] = new OpenPanelCommand(host, MenuPanelId.LoadGame,
                () => SaveSlotManager.HasInstance && SaveSlotManager.Instance.HasAnySave);
            _commands[MenuAction.Options] = new OpenPanelCommand(host, MenuPanelId.Options);
            _commands[MenuAction.Credits] = new OpenPanelCommand(host, MenuPanelId.Credits);
            _commands[MenuAction.QuitGame] = new OpenPanelCommand(host, MenuPanelId.QuitConfirmation);
        }

        private void WireButtons()
        {
            foreach (var button in view.ButtonsInOrder)
            {
                var action = (MenuAction)button.userData;
                button.clicked += () => Activate(action);
                button.RegisterCallback<FocusEvent>(_ =>
                {
                    _lastFocused = button;
                    GameEventsRelay.OnMenuButtonFocused.Invoke(action);
                });
                button.AddManipulator(new MenuButtonFeedback(gameObject));
            }
        }

        private void AttachNavigation()
        {
            _navigationHost = view.PanelRoot;
            if (_navigationHost == null)
            {
                Debug.LogError("[MainMenuPresenter] Menu document isn't attached to a panel; keyboard navigation disabled.", this);
                return;
            }

            // NOTE: Esc on the root menu opens the Quit confirmation.
            _navigation = new MenuNavigation(view.ButtonsInOrder, () => _isInteractive, () => Activate(MenuAction.QuitGame));
            _navigationHost.AddManipulator(_navigation);
        }

        private async void OnMainMenuReady()
        {
            if (_isShowing || _isInteractive)
                return;
            _isShowing = true;

            try
            {
                RefreshAvailability(); // NOTE: After the pan the loader is idle, so availability reflects real state.
                await entranceAnimator.PlayAsync(destroyCancellationToken);

                SetInteractive(true);
                FocusFirstAvailable();
            }
            catch (OperationCanceledException)
            {
                // Scene unloaded mid-entrance.
            }
            finally
            {
                _isShowing = false;
            }
        }

        // NOTE: Leaving the scene ends interactivity so Esc/Enter can't fire during the fade-out.
        private void OnSceneLoadStarted(SceneId _) => SetInteractive(false);

        // NOTE: Also runs when save slots change (e.g. the last save was deleted), so Continue/Load gray out in time.
        private void RefreshAvailability()
        {
            foreach (var button in view.ButtonsInOrder)
            {
                var action = (MenuAction)button.userData;
                var available = _commands.TryGetValue(action, out var command) && command.IsAvailable;
                button.SetEnabled(available); // Disabled = greyed (USS :disabled) and skipped by navigation.
            }
        }

        private void FocusFirstAvailable()
        {
            foreach (var button in view.ButtonsInOrder)
            {
                if (button.enabledInHierarchy && button.focusable)
                {
                    button.Focus();
                    return;
                }
            }
        }

        private void BlurIfFocusedInMenu()
        {
            if (view.Root?.focusController?.focusedElement is VisualElement focused && view.Root.Contains(focused))
                focused.Blur();
        }

        private void Activate(MenuAction action)
        {
            if (!_isInteractive || !_commands.TryGetValue(action, out var command))
                return;
            if (!command.IsAvailable || !command.CanExecute)
                return;

            GameEventsRelay.OnMenuActionInvoked.Invoke(action);
            command.Execute();
        }
    }
}