// Intro state: fades in the naming panel, keeps the name valid, asks "Are you sure?", saves the guild, then enters the game.
using System;
using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class IntroNamingState : IState
    {
        private readonly GuildNamingView _view;
        private readonly GuildNameRules _rules;
        private readonly NewGuildSaver _saver;
        private readonly GameObject _linkTarget;
        private readonly CancellationToken _token;
        private GuildNameInputFilter _filter;
        private TextFieldNavigationGuard _fieldGuard;
        private MenuNavigation _navigation;
        private MenuNavigation _confirmNavigation;
        private VisualElement _navigationHost;
        private Tween _fade;
        private bool _isActive;
        private bool _isSubmitting;

        public IntroNamingState(GuildNamingView view, GuildNameRules rules, NewGuildSaver saver,
                                GameObject linkTarget, CancellationToken token)
        {
            _view = view;
            _rules = rules;
            _saver = saver;
            _linkTarget = linkTarget;
            _token = token;
        }

        private InlineConfirmation Confirmation => _view.Confirmation;

        // The naming panel itself accepts input (not fading, not saving, no prompt open).
        private bool CanAct => _isActive && !_isSubmitting && _view.IsInteractable && !Confirmation.IsOpen;

        // The "Are you sure?" prompt accepts input.
        private bool CanAnswer => _isActive && !_isSubmitting && Confirmation.IsOpen;

        public void Enter()
        {
            _isActive = true;
            _isSubmitting = false;

            _view.ResetView(_rules.MaxLength);
            _filter = new GuildNameInputFilter(_view.Field, _rules, OnNameChanged);
            _view.Confirm.clicked += RequestConfirmation;
            _view.Field.RegisterCallback<KeyDownEvent>(OnFieldKeyDown, TrickleDown.TrickleDown);

            // NOTE: While typing, navigation (which includes W/A/S/D) must not move focus — the guard swallows it, and
            // MenuNavigation stays idle. Real ↓ leaves the field; from Confirm, ↑ comes back.
            _fieldGuard = new TextFieldNavigationGuard(FocusConfirm);
            _view.Field.AddManipulator(_fieldGuard);

            _navigationHost = _view.PanelRoot;
            _navigation = new MenuNavigation(_view.Focusables, () => CanAct && !_view.IsFieldFocused, null, NavigationAxis.Vertical);

            // NOTE: Left/Right between No and Yes; Esc answers No.
            _confirmNavigation = new MenuNavigation(Confirmation.Buttons, () => CanAnswer, Confirmation.Cancel, NavigationAxis.Horizontal);

            _navigationHost?.AddManipulator(_navigation);
            _navigationHost?.AddManipulator(_confirmNavigation);

            ShowAsync();
        }

        public void Exit()
        {
            _isActive = false;
            KillFade();
            _filter?.Dispose();
            _filter = null;

            // NOTE: May run during scene unload, after the view is already destroyed.
            if (_view && _view.IsBound)
            {
                Confirmation.HideImmediate();
                _view.Confirm.clicked -= RequestConfirmation;
                _view.Field.UnregisterCallback<KeyDownEvent>(OnFieldKeyDown, TrickleDown.TrickleDown);
                if (_fieldGuard != null)
                    _view.Field.RemoveManipulator(_fieldGuard);
            }
            _fieldGuard = null;

            if (_navigationHost != null)
            {
                if (_navigation != null)
                    _navigationHost.RemoveManipulator(_navigation);
                if (_confirmNavigation != null)
                    _navigationHost.RemoveManipulator(_confirmNavigation);
            }
            _navigation = null;
            _confirmNavigation = null;
        }

        // NOTE: Input stays locked until the fade-in finishes, then focus goes straight to the name field.
        private async void ShowAsync()
        {
            try
            {
                _view.SetInteractable(false);
                _view.SetVisible(true);
                _view.Opacity = 0f;

                KillFade();
                _fade = DOVirtual.Float(0f, 1f, GameConfig.Instance.Intro.NamingFadeDuration, value => _view.Opacity = value)
                    .SetEase(Ease.OutSine)
                    .SetUpdate(true)
                    .SetLink(_linkTarget);

                await _fade.AwaitCompletionAsync(_token);
                if (!_isActive)
                    return;

                _view.Opacity = 1f;
                _view.SetInteractable(true);
                _view.Field.Focus();
            }
            catch (OperationCanceledException)
            {
                // Scene unloaded mid-fade.
            }
        }

        private void OnNameChanged(string value)
        {
            _view.SetCounter(value.Length, _rules.MaxLength);
            _view.SetError(null);
            _view.Confirm.SetEnabled(_rules.IsValid(value));
        }

        // NOTE: Enter inside a text field isn't a navigation submit, so it's caught here before the field handles it.
        private void OnFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            evt.StopPropagation();
            RequestConfirmation();
        }

        private void FocusConfirm()
        {
            if (CanAct && _view.Confirm.enabledInHierarchy)
                _view.Confirm.Focus();
        }

        // NOTE: No is pre-selected (like the Quit popup), so an accidental double Enter doesn't lock in the name.
        private void RequestConfirmation()
        {
            if (!CanAct)
                return;

            var guildName = _rules.Finalize(_view.Field.value);
            if (!_rules.IsValid(guildName))
                return;

            var confirmed = false;
            _view.SetInteractable(false);
            Confirmation.Show($"Are you sure?\n{guildName}",
                              onConfirm: () => confirmed = true,
                              onClosed: () =>
                              {
                                  if (!_isActive) return;

                                  if (confirmed)
                                      Save(guildName);
                                  else
                                      Unlock(null); // No / Esc: back to the field, name kept.
                              });
        }

        // NOTE: The submit gate is set before anything else, so the save and transition can only start once.
        private void Save(string guildName)
        {
            if (_isSubmitting) return;
            _isSubmitting = true;

            if (!_saver.TryCreate(guildName, out var error))
            {
                Unlock(error);
                return;
            }

            if (!GameSessionLauncher.EnterGame())
                Unlock("Can't continue right now. Try again.");
        }

        private void Unlock(string error)
        {
            _isSubmitting = false;
            _view.SetError(error);
            _view.SetInteractable(true);
            _view.Field.Focus();
        }

        private void KillFade()
        {
            if (_fade.IsActive())
                _fade.Kill();
        }
    }
}