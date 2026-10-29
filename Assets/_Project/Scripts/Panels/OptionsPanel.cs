using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class OptionsPanel : MenuPanel
    {
        private const string RestoreName = "options-restore";
        private const string ApplyName = "options-apply";
        private const string BackName = "options-back";
        private const string DropdownLayerName = "options-dropdown-layer";
        private const string DisplayPageName = "tab-page-display";

        // NOTE: Lets Screen.width/height settle after a mode change before the Display lists re-read them.
        private const long DisplayRefreshDelayMs = 300;

        private readonly List<Button> _buttons = new();
        private readonly List<VisualElement> _focusOrder = new(); // NOTE: Shared with MenuNavigation; rebuilt per tab.
        private OptionsTabBar _tabs;
        private AudioSettingsView _audio;
        private DisplayModeView _displayMode;
        private DisplayQualityView _displayQuality;
        private InlineConfirmation _confirm;
        private KeepSettingsPrompt _keepPrompt;
        private VisualElement _dropdownLayer;
        private ScrollView _displayPage;
        private Button _restore;
        private Button _apply;
        private Button _back;
        private bool _escDismissesConfirm;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override Focusable DefaultFocus => _tabs.SelectedButton;

        private bool IsDropdownOpen => _displayMode.Resolution.IsOpen;
        private bool IsPanelActive => IsInteractable && !_confirm.IsOpen && !IsDropdownOpen;
        private bool IsDropdownActive => IsInteractable && IsDropdownOpen && !_confirm.IsOpen;
        private bool IsConfirmActive => IsInteractable && _confirm.IsOpen;
        private bool HasUnappliedDisplay => _tabs.Selected == SettingsTab.Display && _displayMode.HasPendingChanges;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _tabs = OptionsTabBar.Bind(documentRoot);
            _audio = AudioSettingsView.Bind(documentRoot);
            _displayMode = DisplayModeView.Bind(documentRoot);
            _displayQuality = DisplayQualityView.Bind(documentRoot);
            _confirm = InlineConfirmation.Bind(documentRoot);
            _dropdownLayer = documentRoot.Q<VisualElement>(DropdownLayerName);
            _displayPage = documentRoot.Q<ScrollView>(DisplayPageName);
            _restore = documentRoot.Q<Button>(RestoreName);
            _apply = documentRoot.Q<Button>(ApplyName);
            _back = documentRoot.Q<Button>(BackName);

            _buttons.Clear();
            if (_tabs == null || _audio == null || _displayMode == null || _displayQuality == null || _confirm == null ||
                _dropdownLayer == null || _displayPage == null || _restore == null || _apply == null || _back == null)
                return false;

            _buttons.AddRange(_tabs.Buttons);
            _buttons.Add(_restore);
            _buttons.Add(_apply);
            _buttons.Add(_back);
            _buttons.AddRange(_confirm.Buttons);
            return true;
        }

        protected override void OnInitialize()
        {
            var audioConfig = GameConfig.Instance.PlayerSettings;
            var displayConfig = GameConfig.Instance.DisplaySettings;

            _tabs.Initialize(OnTabSelected, () => IsPanelActive, CanLeaveTab);
            _audio.Initialize(audioConfig.VolumeStep);
            _displayMode.Initialize(_dropdownLayer, gameObject, UpdateApplyState);
            _displayQuality.Initialize(displayConfig.BrightnessStep);
            _confirm.Initialize();
            _keepPrompt = new KeepSettingsPrompt(_confirm, Root);

            _restore.clicked += OnRestoreClicked;
            _apply.clicked += OnApplyClicked;
            _back.clicked += RequestClose;
            _displayPage.RegisterCallback<FocusInEvent>(OnDisplayPageFocusIn);

            // NOTE: Registration order matters. Slider stepping runs before list navigation on the same root.
            AddPanelManipulator(new SliderStepNavigation(_audio.Sliders, () => IsPanelActive, audioConfig.VolumeStep));
            AddPanelManipulator(new SliderStepNavigation(_displayQuality.BrightnessSliders, () => IsPanelActive, displayConfig.BrightnessStep));
            AddPanelManipulator(new SliderStepNavigation(_displayQuality.FpsSliders, () => IsPanelActive, 1));
            AddPanelManipulator(new MenuNavigation(_focusOrder, () => IsPanelActive, null, passThroughCrossAxis: true));
            AddPanelManipulator(new MenuNavigation(_displayMode.Resolution.Items, () => IsDropdownActive, null));
            AddPanelManipulator(new MenuNavigation(_confirm.Buttons, () => IsConfirmActive, null, NavigationAxis.Horizontal));

            GameEventsRelay.OnSettingsApplied.AddListener(RefreshAll);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnSettingsApplied.RemoveListener(RefreshAll);
            base.OnDestroy();
        }

        // NOTE: Every open starts on the Audio tab with fresh values, nothing staged and no overlay showing.
        protected override void OnOpening()
        {
            _confirm.HideImmediate();
            _escDismissesConfirm = false;
            _displayMode.Resolution.Close(refocus: false);
            _tabs.Select(SettingsTab.Audio);
            RefreshAll();
        }

        // NOTE: Esc answers an open overlay first (No / Revert, or "stay" on Apply-changes), then closes the dropdown,
        // then leaves the panel (asking about unapplied display changes).
        protected override void OnCancel()
        {
            if (_confirm.IsOpen)
            {
                if (_escDismissesConfirm) _confirm.Dismiss();
                else _confirm.Cancel();
            }
            else if (IsDropdownOpen)
            {
                _displayMode.Resolution.Close();
            }
            else
            {
                RequestClose();
            }
        }

        private void OnTabSelected(SettingsTab tab)
        {
            var isDisplay = tab == SettingsTab.Display;

            _focusOrder.Clear();
            _focusOrder.Add(_tabs.SelectedButton);

            if (tab == SettingsTab.Audio)
            {
                _focusOrder.AddRange(_audio.Sliders);
            }
            else if (isDisplay)
            {
                _focusOrder.AddRange(_displayMode.Focusables);
                _focusOrder.AddRange(_displayQuality.Focusables);
            }

            _focusOrder.Add(_restore);
            if (isDisplay)
                _focusOrder.Add(_apply);
            _focusOrder.Add(_back);

            // NOTE: Apply exists only on the Display tab.
            _apply.style.display = isDisplay ? DisplayStyle.Flex : DisplayStyle.None;
            _restore.SetEnabled(SettingsManager.HasInstance && SettingsManager.Instance.HasDefaults(tab));
            UpdateApplyState();
        }

        // NOTE: Holds a tab switch back while display changes are unapplied; asks first, then switches.
        private bool CanLeaveTab(SettingsTab target)
        {
            if (!HasUnappliedDisplay)
                return true;

            AskAboutUnapplied(() =>
            {
                _tabs.Select(target);
                _tabs.SelectedButton.Focus();
            });
            return false;
        }

        // NOTE: Keeps the focused row visible on the scrolling Display page.
        private void OnDisplayPageFocusIn(FocusInEvent evt)
        {
            if (evt.target is VisualElement element && element != _displayPage)
                _displayPage.ScrollTo(element);
        }

        private void UpdateApplyState() => _apply.SetEnabled(_displayMode.HasPendingChanges);

        private void OnApplyClicked()
        {
            if (!IsPanelActive || !_displayMode.HasPendingChanges)
                return;

            // NOTE: Apply grays out once applied, so focus moves on to Back.
            ApplyPending(() => _back.Focus());
        }

        // Commits the staged screen mode; the keep prompt appears only when it really changes the screen.
        private void ApplyPending(Action then)
        {
            if (!SettingsManager.HasInstance)
            {
                then?.Invoke();
                return;
            }

            var previous = SettingsManager.Instance.ScreenMode;
            var pending = _displayMode.Pending;

            if (ResolutionCatalog.ResolveNative(pending).Equals(ResolutionCatalog.ResolveNative(previous)))
            {
                _displayMode.Refresh();
                UpdateApplyState();
                then?.Invoke();
                return;
            }

            SettingsManager.Instance.SetScreenMode(pending);
            _keepPrompt.Show(GameConfig.Instance.DisplaySettings.KeepPromptSeconds,
                             onRevert: () => SettingsManager.Instance.SetScreenMode(previous),
                             onClosed: () =>
                             {
                                 _displayMode.Refresh(); // Staged mode = whatever is applied now (kept or reverted).
                                 UpdateApplyState();
                                 RefreshDisplayListsLater();
                                 then?.Invoke();
                             });
        }

        // "Apply display changes?" Apply → commits (and keep prompt); Discard → drops them; Esc → stay.
        private void AskAboutUnapplied(Action proceed)
        {
            var applyChosen = false;
            var discardChosen = false;
            var returnFocus = Root.focusController?.focusedElement;

            _escDismissesConfirm = true;
            _confirm.Show("Apply display changes?",
                          onConfirm: () => applyChosen = true,
                          onClosed: () =>
                          {
                              _escDismissesConfirm = false;

                              if (applyChosen)
                              {
                                  ApplyPending(proceed);
                              }
                              else if (discardChosen)
                              {
                                  _displayMode.Refresh();
                                  UpdateApplyState();
                                  proceed?.Invoke();
                              }
                              else
                              {
                                  returnFocus?.Focus(); // Esc: stay where we were, changes still staged.
                              }
                          },
                          confirmLabel: "Apply", cancelLabel: "Discard", focusConfirm: true,
                          onCancelled: () => discardChosen = true);
        }

        private void OnRestoreClicked()
        {
            if (!IsPanelActive || !SettingsManager.HasInstance)
                return;

            var tab = _tabs.Selected;
            var restored = false;

            _confirm.Show($"Restore default {tab.ToString().ToLowerInvariant()}\nsettings?",
                          onConfirm: () =>
                          {
                              SettingsManager.Instance.RestoreDefaults(tab);
                              restored = true;
                          },
                          onClosed: () =>
                          {
                              // NOTE: The default screen mode is staged like any other change; Apply commits it.
                              if (restored && tab == SettingsTab.Display)
                                  _displayMode.SetPending(SettingsManager.Instance.DefaultScreenMode);

                              UpdateApplyState();
                              _restore.Focus();
                          });
        }

        private void RefreshAll()
        {
            _audio.Refresh();
            _displayMode.Refresh();
            _displayQuality.Refresh();
            UpdateApplyState();
        }

        // NOTE: Rebuilds the lists for the new screen size without touching anything the player staged meanwhile.
        private void RefreshDisplayListsLater() => Root.schedule.Execute(() =>
        {
            _displayMode.RefreshLists();
            _displayQuality.Refresh();
            UpdateApplyState();
        }).StartingIn(DisplayRefreshDelayMs);

        private void RequestClose()
        {
            if (!IsPanelActive)
                return;

            if (HasUnappliedDisplay)
                AskAboutUnapplied(CloseNow);
            else
                CloseNow();
        }

        private void CloseNow()
        {
            if (SettingsManager.HasInstance)
                SettingsManager.Instance.SaveIfDirty();

            Host.Close(Id);
        }
    }
}