using System;
using UnityEngine;

namespace WanderersGuild
{
    [Serializable]
    public class DisplaySettingsData
    {
        // NOTE: 0 × 0 means "the monitor's native resolution", resolved when applied.
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private WindowModeSetting windowMode = WindowModeSetting.Borderless;
        [SerializeField] private int monitorIndex;
        [SerializeField] private bool vSync = true;
        [SerializeField] private int fpsLimitIndex;
        [SerializeField] private int brightness = 50;
        [SerializeField] private int uiScaleCap;
        [SerializeField] private bool showFpsCounter;

        public ScreenModeSnapshot ScreenMode
        {
            get => new(width, height, windowMode, monitorIndex);
            set
            {
                width = Mathf.Max(0, value.Width);
                height = Mathf.Max(0, value.Height);
                windowMode = value.WindowMode;
                monitorIndex = Mathf.Max(0, value.MonitorIndex);
            }
        }

        public bool VSync { get => vSync; set => vSync = value; }
        public int FpsLimitIndex { get => fpsLimitIndex; set => fpsLimitIndex = Mathf.Max(0, value); }
        public int Brightness { get => brightness; set => brightness = Mathf.Clamp(value, 0, 100); }
        public int UIScaleCap { get => uiScaleCap; set => uiScaleCap = Mathf.Max(0, value); }
        public bool ShowFpsCounter { get => showFpsCounter; set => showFpsCounter = value; }

        // Defaults from GameConfig: native resolution on the primary monitor.
        public static DisplaySettingsData CreateDefault(DisplaySettingsConfig config) => new()
        {
            windowMode = config.DefaultWindowMode,
            vSync = config.DefaultVSync,
            fpsLimitIndex = config.DefaultFpsLimitIndex,
            brightness = config.DefaultBrightness
        };

        // Clamps every value into range (hand-edited or corrupted files).
        public void Sanitize(DisplaySettingsConfig config)
        {
            width = Mathf.Max(0, width);
            height = Mathf.Max(0, height);
            if (width == 0 || height == 0)
            {
                width = 0;
                height = 0;
            }

            if (!Enum.IsDefined(typeof(WindowModeSetting), windowMode))
                windowMode = config.DefaultWindowMode;

            monitorIndex = Mathf.Max(0, monitorIndex);
            fpsLimitIndex = Mathf.Clamp(fpsLimitIndex, 0, Mathf.Max(0, config.FpsLimitCount - 1));
            brightness = Mathf.Clamp(brightness, 0, 100);
            uiScaleCap = Mathf.Max(0, uiScaleCap);
        }
    }
}