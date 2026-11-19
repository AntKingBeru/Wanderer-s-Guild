// Screen sizes for the Display options: what the monitor offers, its native size, and labels. Without window control (the web)
// there's no monitor info — the canvas size stands in.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class ResolutionCatalog
    {
        // Unique sizes (refresh-rate duplicates removed), sorted largest first. Never empty.
        public static List<Vector2Int> GetAvailable()
        {
            List<Vector2Int> sorted = new();
            if (PlatformInfo.HasWindowControl)
            {
                HashSet<Vector2Int> unique = new();
                foreach (var resolution in Screen.resolutions)
                    unique.Add(new Vector2Int(resolution.width, resolution.height));

                sorted.AddRange(unique);
                sorted.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
            }

            if (sorted.Count == 0)
                sorted.Add(Native());

            return sorted;
        }

        // Native size of the monitor the window is on; the canvas size where there's no window control.
        // NOTE: Screen.mainWindowDisplayInfo throws outside desktop players — never read it without HasWindowControl.
        public static Vector2Int Native()
        {
            if (!PlatformInfo.HasWindowControl)
                return new Vector2Int(Screen.width, Screen.height);

            var display = Screen.mainWindowDisplayInfo;
            return display is { width: > 0, height: > 0 }
                ? new Vector2Int(display.width, display.height)
                : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }

        // Replaces "native" (0 × 0) with the real native size, so two modes can be compared fairly.
        public static ScreenModeSnapshot ResolveNative(ScreenModeSnapshot mode)
        {
            if (mode is { Width: > 0, Height: > 0 })
                return mode;

            var native = Native();
            return mode.WithResolution(native.x, native.y);
        }

        public static string Label(Vector2Int size) => $"{size.x} x {size.y}";
    }
}