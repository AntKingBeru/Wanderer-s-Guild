using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class DisplayQualityView
    {
        private const string VSyncName = "display-vsync";
        private const string FpsName = "display-fps";
        private const string FpsValueName = "display-fps-value";
        private const string BrightnessName = "display-brightness";
        private const string BrightnessValueName = "display-brightness-value";
        private const string UIScaleName = "display-ui-scale";
        private const string FpsCounterName = "display-fps-counter";

        private readonly Toggle _vSync;
        private readonly SliderInt _fps;
        private readonly OutlinedText _fpsValue;
        private readonly SliderInt _brightness;
        private readonly OutlinedText _brightnessValue;
        private readonly OptionCycler _uiScale;
        private readonly Toggle _fpsCounter;
        private readonly List<VisualElement> _focusables;
        private readonly List<SliderInt> _brightnessSliders;
        private readonly List<SliderInt> _fpsSliders;
        private readonly List<string> _uiScaleLabels = new();
        private int _brightnessStep = 5;

        public IReadOnlyList<VisualElement> Focusables => _focusables;
        public IReadOnlyList<SliderInt> BrightnessSliders => _brightnessSliders;
        public IReadOnlyList<SliderInt> FpsSliders => _fpsSliders;

        private DisplayQualityView(Toggle vSync, SliderInt fps, OutlinedText fpsValue, SliderInt brightness,
                                   OutlinedText brightnessValue, OptionCycler uiScale, Toggle fpsCounter)
        {
            _vSync = vSync;
            _fps = fps;
            _fpsValue = fpsValue;
            _brightness = brightness;
            _brightnessValue = brightnessValue;
            _uiScale = uiScale;
            _fpsCounter = fpsCounter;
            _focusables = new List<VisualElement> { vSync, fps, brightness, uiScale, fpsCounter };
            _brightnessSliders = new List<SliderInt> { brightness };
            _fpsSliders = new List<SliderInt> { fps };
        }

        // Finds every control. Returns null if any is missing.
        public static DisplayQualityView Bind(VisualElement root)
        {
            var vSync = root.Q<Toggle>(VSyncName);
            var fps = root.Q<SliderInt>(FpsName);
            var fpsValue = root.Q<OutlinedText>(FpsValueName);
            var brightness = root.Q<SliderInt>(BrightnessName);
            var brightnessValue = root.Q<OutlinedText>(BrightnessValueName);
            var uiScale = root.Q<OptionCycler>(UIScaleName);
            var fpsCounter = root.Q<Toggle>(FpsCounterName);

            var complete = vSync != null && fps != null && fpsValue != null && brightness != null &&
                            brightnessValue != null && uiScale != null && fpsCounter != null;
            return complete ? new DisplayQualityView(vSync, fps, fpsValue, brightness, brightnessValue, uiScale, fpsCounter) : null;
        }

        public void Initialize(int brightnessStep)
        {
            _brightnessStep = Mathf.Max(1, brightnessStep);

            _fps.lowValue = 0;
            _fps.highValue = Mathf.Max(0, GameConfig.Instance.DisplaySettings.FpsLimitCount - 1);
            _brightness.lowValue = 0;
            _brightness.highValue = 100;

            _vSync.RegisterValueChangedCallback(evt =>
            {
                if (SettingsManager.HasInstance) SettingsManager.Instance.SetVSync(evt.newValue);
                UpdateFpsAvailability();
            });

            _fps.RegisterValueChangedCallback(evt =>
            {
                if (SettingsManager.HasInstance) SettingsManager.Instance.SetFpsLimitIndex(evt.newValue);
                _fpsValue.Text = FpsLabel(evt.newValue);
            });

            _brightness.RegisterValueChangedCallback(evt => OnBrightnessChanged(evt.newValue));

            _fpsCounter.RegisterValueChangedCallback(evt =>
            {
                if (SettingsManager.HasInstance) SettingsManager.Instance.SetShowFpsCounter(evt.newValue);
            });
        }

        // Pulls current values without triggering change callbacks; rebuilds the UI scale choices.
        public void Refresh()
        {
            if (!SettingsManager.HasInstance)
                return;

            var settings = SettingsManager.Instance;
            _vSync.SetValueWithoutNotify(settings.VSync);
            _fps.SetValueWithoutNotify(settings.FpsLimitIndex);
            _fpsValue.Text = FpsLabel(settings.FpsLimitIndex);
            _brightness.SetValueWithoutNotify(settings.Brightness);
            _brightnessValue.Text = settings.Brightness.ToString();
            _fpsCounter.SetValueWithoutNotify(settings.ShowFpsCounter);
            RefreshUIScale(settings.UIScaleCap);
            UpdateFpsAvailability();
        }

        // NOTE: Choice 0 = Auto (cap 0); choice N = N×. The list depends on the current screen size.
        private void RefreshUIScale(int cap)
        {
            var max = PixelUIScaler.HasInstance ? PixelUIScaler.Instance.MaxScaleForScreen : 1;

            _uiScaleLabels.Clear();
            _uiScaleLabels.Add("Auto");
            for (var scale = 1; scale <= max; scale++)
                _uiScaleLabels.Add($"{scale}x");

            _uiScale.SetOptions(_uiScaleLabels, cap <= 0 ? 0 : Mathf.Min(cap, max), index =>
            {
                if (SettingsManager.HasInstance) SettingsManager.Instance.SetUIScaleCap(index);
            });
            _uiScale.SetEnabled(max > 1);
        }

        // NOTE: VSync paces frames by itself, so the limit is disabled (and skipped by navigation) while it's on.
        private void UpdateFpsAvailability() => _fps.SetEnabled(!_vSync.value);

        // NOTE: Mouse drags produce every integer; snapping keeps values on the step grid the player sees.
        private void OnBrightnessChanged(int value)
        {
            var snapped = Mathf.Clamp(Mathf.RoundToInt(value / (float)_brightnessStep) * _brightnessStep, 0, 100);
            if (snapped != value)
                _brightness.SetValueWithoutNotify(snapped);

            _brightnessValue.Text = snapped.ToString();
            if (SettingsManager.HasInstance) SettingsManager.Instance.SetBrightness(snapped);
        }

        private static string FpsLabel(int index)
        {
            var limit = GameConfig.Instance.DisplaySettings.FpsLimitAt(index);
            return limit <= 0 ? "Unlimited" : limit.ToString();
        }
    }
}