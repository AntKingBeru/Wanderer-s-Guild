using System;
using UnityEngine;

namespace WanderersGuild
{
    public class SettingsManager : Singleton<SettingsManager>
    {
        private static readonly AudioChannel[] AllChannels =
        {
            AudioChannel.Master, AudioChannel.Music, AudioChannel.Sfx, AudioChannel.Character
        };

        private GameSettingsData _current;
        private AudioSettingsApplier _audioApplier;
        private DisplayModeApplier _displayModeApplier;
        private FrameRateApplier _frameRateApplier;
        private bool _isDirty;

        protected override void OnSingletonAwake()
        {
            _audioApplier = new AudioSettingsApplier(GameConfig.Instance.AudioMixer);
            _displayModeApplier = new DisplayModeApplier();
            _frameRateApplier = new FrameRateApplier();
            _current = CreateDefaults();
        }

        // Loads settings.json (or defaults) and applies everything. Called once at boot by LoadSettingsStep.
        public void LoadAndApply()
        {
            if (SettingsStore.TryLoad(GameConfig.Instance.PlayerSettings.FileName, out var loaded))
            {
                loaded.Sanitize(GameConfig.Instance.DisplaySettings);
                _current = loaded;
            }
            else
            {
                _current = CreateDefaults();
            }

            _audioApplier.ApplyAll(_current.Audio);
            ApplyDisplay();
            _isDirty = false;
            GameEventsRelay.OnSettingsApplied.Invoke();
        }

        #region Audio
        public int GetVolume(AudioChannel channel) => _current.Audio.Get(channel);

        // Applies immediately; written to disk on the next SaveIfDirty (Options close, or quit).
        public void SetVolume(AudioChannel channel, int percent)
        {
            var value = Mathf.Clamp(percent, 0, 100);
            if (_current.Audio.Get(channel) == value)
                return;

            _current.Audio.Set(channel, value);
            _audioApplier.Apply(channel, value);
            _isDirty = true;
        }
        #endregion

        #region Display
        public ScreenModeSnapshot ScreenMode => _current.Display.ScreenMode;
        public bool VSync => _current.Display.VSync;
        public int FpsLimitIndex => _current.Display.FpsLimitIndex;
        public int Brightness => _current.Display.Brightness;
        public int UIScaleCap => _current.Display.UIScaleCap;
        public bool ShowFpsCounter => _current.Display.ShowFpsCounter;
        public int MonitorCount => _displayModeApplier.MonitorCount;

        // The configured default screen mode (native resolution, default window mode, primary monitor).
        public ScreenModeSnapshot DefaultScreenMode =>
            DisplaySettingsData.CreateDefault(GameConfig.Instance.DisplaySettings).ScreenMode;

        // Resolution + window mode + monitor. Options calls this from Apply and shows the keep prompt.
        public void SetScreenMode(ScreenModeSnapshot mode)
        {
            if (_current.Display.ScreenMode.Equals(mode))
                return;

            _current.Display.ScreenMode = mode;
            ApplyScreenMode(_current.Display.ScreenMode);
            _isDirty = true;
        }

        public void SetVSync(bool active)
        {
            if (_current.Display.VSync == active)
                return;

            _current.Display.VSync = active;
            ApplyFrameRate();
            _isDirty = true;
        }

        public void SetFpsLimitIndex(int index)
        {
            var clamped = Mathf.Clamp(index, 0, Mathf.Max(0, GameConfig.Instance.DisplaySettings.FpsLimitCount - 1));
            if (_current.Display.FpsLimitIndex == clamped)
                return;

            _current.Display.FpsLimitIndex = clamped;
            ApplyFrameRate();
            _isDirty = true;
        }

        public void SetBrightness(int percent)
        {
            var value = Mathf.Clamp(percent, 0, 100);
            if (_current.Display.Brightness == value)
                return;

            _current.Display.Brightness = value;
            ApplyBrightness();
            _isDirty = true;
        }

        // 0 = automatic (largest scale that fits); otherwise the maximum integer UI scale.
        public void SetUIScaleCap(int cap)
        {
            var value = Mathf.Max(0, cap);
            if (_current.Display.UIScaleCap == value)
                return;

            _current.Display.UIScaleCap = value;
            ApplyUIScale();
            _isDirty = true;
        }

        public void SetShowFpsCounter(bool show)
        {
            if (_current.Display.ShowFpsCounter == show)
                return;

            _current.Display.ShowFpsCounter = show;
            ApplyFpsCounter();
            _isDirty = true;
        }
        #endregion

        #region Defaults and persistence
        // True when the tab has settings that Restore Defaults can reset.
        public bool HasDefaults(SettingsTab tab) => tab is SettingsTab.Audio or SettingsTab.Display;

        // Restores a tab's defaults. On Display, the screen mode is kept: Options stages its default for Apply.
        public void RestoreDefaults(SettingsTab tab)
        {
            switch (tab)
            {
                case SettingsTab.Audio:
                    ApplyAudioDefaults(_current.Audio);
                    _audioApplier.ApplyAll(_current.Audio);
                    break;

                case SettingsTab.Display:
                    ScreenModeSnapshot keptMode = _current.Display.ScreenMode;
                    _current.Display = DisplaySettingsData.CreateDefault(GameConfig.Instance.DisplaySettings);
                    _current.Display.ScreenMode = keptMode; // NOTE: Screen-mode changes always go through Apply + the keep prompt.
                    ApplyDisplay();
                    break;

                default:
                    return;
            }

            _isDirty = true;
            GameEventsRelay.OnSettingsApplied.Invoke();
        }

        public void SaveIfDirty()
        {
            if (!_isDirty)
                return;

            if (SettingsStore.TrySave(GameConfig.Instance.PlayerSettings.FileName, _current))
                _isDirty = false;
        }

        // NOTE: Covers quitting from anywhere, including stopping Play Mode in the Editor.
        private void OnApplicationQuit() => SaveIfDirty();
        #endregion

        #region Applying
        private void ApplyDisplay()
        {
            ApplyScreenMode(_current.Display.ScreenMode);
            ApplyFrameRate();
            ApplyBrightness();
            ApplyUIScale();
            ApplyFpsCounter();
        }

        // NOTE: async void entry point; the applier itself makes sure the newest request wins.
        private async void ApplyScreenMode(ScreenModeSnapshot mode)
        {
            try
            {
                await _displayModeApplier.ApplyAsync(mode, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Quitting mid-apply.
            }
        }

        private void ApplyFrameRate()
        {
            var config = GameConfig.Instance.DisplaySettings;
            _frameRateApplier.Apply(_current.Display.VSync, config.FpsLimitAt(_current.Display.FpsLimitIndex));
        }

        // NOTE: 50 = no change; 0 / 100 = minus / plus the configured max exposure.
        private void ApplyBrightness()
        {
            if (!BrightnessController.HasInstance)
                return;

            var maxExposure = GameConfig.Instance.DisplaySettings.MaxExposure;
            BrightnessController.Instance.SetExposure((_current.Display.Brightness - 50) / 50f * maxExposure);
        }

        private void ApplyUIScale()
        {
            if (PixelUIScaler.HasInstance)
                PixelUIScaler.Instance.SetScaleCap(_current.Display.UIScaleCap);
        }

        private void ApplyFpsCounter()
        {
            if (FpsCounter.HasInstance)
                FpsCounter.Instance.SetVisible(_current.Display.ShowFpsCounter);
        }
        #endregion

        private static GameSettingsData CreateDefaults()
        {
            GameSettingsData data = new();
            ApplyAudioDefaults(data.Audio);
            data.Display = DisplaySettingsData.CreateDefault(GameConfig.Instance.DisplaySettings);
            return data;
        }

        private static void ApplyAudioDefaults(AudioSettingsData audio)
        {
            var config = GameConfig.Instance.PlayerSettings;
            foreach (var channel in AllChannels)
                audio.Set(channel, config.DefaultVolume(channel));
        }
    }
}