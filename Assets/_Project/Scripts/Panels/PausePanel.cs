// In-game Esc menu: Resume, Save, Options, Save & Quit to Menu, Save & Quit to Desktop (asks only if the save fails).
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class PausePanel : MenuPanel
    {
        private const string ResumeName = "pause-resume";
        private const string SaveName = "pause-save";
        private const string OptionsName = "pause-options";
        private const string QuitMenuName = "pause-quit-menu";
        private const string QuitDesktopName = "pause-quit-desktop";

        private readonly List<Button> _buttons = new();
        private readonly List<Button> _menuButtons = new();
        private readonly GameExitFlow _exit = new();
        private Button _resume;
        private Button _save;
        private Button _options;
        private Button _quitMenu;
        private Button _quitDesktop;
        private InlineConfirmation _confirm;
        private Focusable _nextFocus;
        private bool _isLeaving;

        protected override IReadOnlyList<Button> Buttons => _buttons;

        // NOTE: Coming back from Options re-selects Options; otherwise Resume. Read once per open.
        protected override Focusable DefaultFocus
        {
            get
            {
                var focus = _nextFocus ?? _resume;
                _nextFocus = null;
                return focus;
            }
        }

        private static bool IsSaving => SaveGameService.HasInstance && SaveGameService.Instance.IsSaving;
        private static bool CanSave => SaveGameService.HasInstance && SaveGameService.Instance.CanSave;
        private bool IsPanelActive => IsInteractable && !_isLeaving && !_confirm.IsOpen && !IsSaving;
        private bool IsConfirmActive => IsInteractable && _confirm.IsOpen;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _resume = documentRoot.Q<Button>(ResumeName);
            _save = documentRoot.Q<Button>(SaveName);
            _options = documentRoot.Q<Button>(OptionsName);
            _quitMenu = documentRoot.Q<Button>(QuitMenuName);
            _quitDesktop = documentRoot.Q<Button>(QuitDesktopName);
            _confirm = InlineConfirmation.Bind(documentRoot);

            _buttons.Clear();
            _menuButtons.Clear();
            if (_resume == null || _save == null || _options == null || _quitMenu == null || _quitDesktop == null || _confirm == null)
                return false;

            _menuButtons.AddRange(new[] { _resume, _save, _options, _quitMenu, _quitDesktop });
            _buttons.AddRange(_menuButtons);
            _buttons.AddRange(_confirm.Buttons);
            return true;
        }

        protected override void OnInitialize()
        {
            _resume.clicked += Resume;
            _save.clicked += SaveGame;
            _options.clicked += OpenOptions;
            _quitMenu.clicked += () => QuitTo(ExitDestination.MainMenu, _quitMenu);
            _quitDesktop.clicked += () => QuitTo(ExitDestination.Desktop, _quitDesktop);
            _confirm.Initialize();

            AddPanelManipulator(new MenuNavigation(_menuButtons, () => IsPanelActive, null));
            AddPanelManipulator(new MenuNavigation(_confirm.Buttons, () => IsConfirmActive, null, NavigationAxis.Horizontal));

            GameEventsRelay.OnGameSaveStarted.AddListener(OnSaveStarted);
            GameEventsRelay.OnGameSaveCompleted.AddListener(OnSaveCompleted);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnGameSaveStarted.RemoveListener(OnSaveStarted);
            GameEventsRelay.OnGameSaveCompleted.RemoveListener(OnSaveCompleted);
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            _confirm.HideImmediate();
            _isLeaving = false;
            RefreshButtons();
        }

        // NOTE: Esc answers the failed-save prompt (No) first; otherwise it resumes the game.
        protected override void OnCancel()
        {
            if (_isLeaving)
                return;

            if (_confirm.IsOpen)
                _confirm.Cancel();
            else
                Resume();
        }

        private void Resume()
        {
            if (IsPanelActive)
                Host.Close(Id);
        }

        private async void SaveGame()
        {
            if (!IsPanelActive || !CanSave)
                return;

            await SaveGameService.Instance.SaveAsync(SaveReason.Manual);
            if (!this)
                return; // NOTE: Scene unloaded while saving.

            RefreshButtons();
            _save.Focus();
        }

        private void OpenOptions()
        {
            if (!IsPanelActive)
                return;

            _nextFocus = _options;
            Host.Open(MenuPanelId.Options);
        }

        // NOTE: Saves first; only a failed save asks — "Couldn't save. Quit anyway?" (No pre-selected).
        private async void QuitTo(ExitDestination destination, Button returnFocus)
        {
            if (!IsPanelActive)
                return;

            _isLeaving = true;
            RefreshButtons();

            var saved = await _exit.TrySaveAsync();
            if (!this)
                return;

            if (saved)
            {
                _exit.Leave(destination);
                return;
            }

            _isLeaving = false;
            RefreshButtons();

            var quitAnyway = false;
            _confirm.Show("Couldn't save.\nQuit anyway?",
                          onConfirm: () => quitAnyway = true,
                          onClosed: () =>
                          {
                              if (quitAnyway)
                              {
                                  _isLeaving = true;
                                  RefreshButtons();
                                  _exit.Leave(destination);
                              }
                              else
                              {
                                  returnFocus.Focus();
                              }
                          });
        }

        private void OnSaveStarted(SaveReason _) => RefreshButtons();
        private void OnSaveCompleted(SaveReason _, bool __) => RefreshButtons();

        // NOTE: Everything locks while saving or leaving; Save also needs an active slot.
        private void RefreshButtons()
        {
            var locked = _isLeaving || IsSaving;
            foreach (var button in _menuButtons)
                button.SetEnabled(!locked);

            _save.SetEnabled(!locked && CanSave);
        }
    }
}