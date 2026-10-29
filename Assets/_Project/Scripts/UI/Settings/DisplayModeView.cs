using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class DisplayModeView
    {
        private const string ResolutionName = "display-resolution";
        private const string WindowModeName = "display-window-mode";
        private const string MonitorName = "display-monitor";

        // NOTE: The order matches WindowModeSetting.
        private static readonly string[] WindowModeLabels = { "Fullscreen", "Borderless", "Windowed" };

        private readonly PixelDropdown _resolution;
        private readonly OptionCycler _windowMode;
        private readonly OptionCycler _monitor;
        private readonly List<VisualElement> _focusables;
        private readonly List<Vector2Int> _resolutions = new();
        private readonly List<string> _resolutionLabels = new();
        private readonly List<string> _monitorLabels = new();
        private ScreenModeSnapshot _pending;
        private Action _onPendingChanged;

        public PixelDropdown Resolution => _resolution;
        public IReadOnlyList<VisualElement> Focusables => _focusables;

        // The staged (not yet applied) screen mode shown by the controls.
        public ScreenModeSnapshot Pending => _pending;

        // True when the staged mode differs from the applied one ("native" is resolved before comparing).
        public bool HasPendingChanges =>
            SettingsManager.HasInstance &&
            !ResolutionCatalog.ResolveNative(_pending).Equals(ResolutionCatalog.ResolveNative(SettingsManager.Instance.ScreenMode));

        private DisplayModeView(PixelDropdown resolution, OptionCycler windowMode, OptionCycler monitor)
        {
            _resolution = resolution;
            _windowMode = windowMode;
            _monitor = monitor;
            _focusables = new List<VisualElement> { resolution, windowMode, monitor };
        }

        // Finds the three controls. Returns null if any is missing.
        public static DisplayModeView Bind(VisualElement root)
        {
            var resolution = root.Q<PixelDropdown>(ResolutionName);
            var windowMode = root.Q<OptionCycler>(WindowModeName);
            var monitor = root.Q<OptionCycler>(MonitorName);

            return resolution != null && windowMode != null && monitor != null
                ? new DisplayModeView(resolution, windowMode, monitor)
                : null;
        }

        /// <param name="dropdownLayer"></param>
        /// <param name="linkTarget"></param>
        /// <param name="onPendingChanged">Runs whenever the staged mode changes (e.g. to enable Apply).</param>
        public void Initialize(VisualElement dropdownLayer, GameObject linkTarget, Action onPendingChanged)
        {
            _resolution.Initialize(dropdownLayer, linkTarget);
            _onPendingChanged = onPendingChanged;
        }

        // Discards any staged change: resets to the applied mode and rebuilds every control.
        public void Refresh()
        {
            if (!SettingsManager.HasInstance)
                return;

            _pending = SettingsManager.Instance.ScreenMode;
            RefreshLists();
        }

        // Rebuilds the option lists (e.g. after the screen size changed) while keeping the staged mode.
        public void RefreshLists()
        {
            if (!SettingsManager.HasInstance)
                return;

            RefreshResolutions();
            _windowMode.SetOptions(WindowModeLabels, (int)_pending.WindowMode,
                                   index => Stage(_pending.WithWindowMode((WindowModeSetting)index)));
            RefreshMonitors();
        }

        // Stages a mode from outside (e.g. Restore Defaults) and shows it. Apply commits it.
        public void SetPending(ScreenModeSnapshot mode)
        {
            _pending = mode;
            RefreshLists();
            _onPendingChanged?.Invoke();
        }

        // NOTE: The control already shows the new value, so staging from a control doesn't rebuild anything.
        private void Stage(ScreenModeSnapshot mode)
        {
            _pending = mode;
            _onPendingChanged?.Invoke();
        }

        private void RefreshResolutions()
        {
            _resolutions.Clear();
            _resolutions.AddRange(ResolutionCatalog.GetAvailable());

            var current = _pending.Width > 0 ? new Vector2Int(_pending.Width, _pending.Height) : ResolutionCatalog.Native();
            var index = _resolutions.IndexOf(current);
            if (index < 0)
            {
                // NOTE: Keeps a saved size selectable even if this monitor doesn't list it.
                _resolutions.Insert(0, current);
                index = 0;
            }

            _resolutionLabels.Clear();
            foreach (var size in _resolutions)
                _resolutionLabels.Add(ResolutionCatalog.Label(size));

            _resolution.SetOptions(_resolutionLabels, index,
                                   i => Stage(_pending.WithResolution(_resolutions[i].x, _resolutions[i].y)));
        }

        private void RefreshMonitors()
        {
            var count = SettingsManager.Instance.MonitorCount;

            _monitorLabels.Clear();
            for (var i = 0; i < count; i++)
                _monitorLabels.Add($"Monitor {i + 1}");

            _monitor.SetOptions(_monitorLabels, Mathf.Clamp(_pending.MonitorIndex, 0, count - 1),
                                index => Stage(_pending.WithMonitor(index)));
            _monitor.SetEnabled(count > 1);
        }
    }
}