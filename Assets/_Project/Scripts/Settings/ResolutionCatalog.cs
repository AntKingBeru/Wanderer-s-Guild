using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class ResolutionCatalog
    {
        // Unique sizes (refresh-rate duplicates removed), sorted largest first. Never empty.
        public static List<Vector2Int> GetAvailable()
        {
            HashSet<Vector2Int> unique = new();
            foreach (var resolution in Screen.resolutions)
                unique.Add(new Vector2Int(resolution.width, resolution.height));

            List<Vector2Int> sorted = new(unique);
            sorted.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));

            if (sorted.Count == 0)
                sorted.Add(Native());

            return sorted;
        }

        // Native size of the monitor the window is currently on.
        public static Vector2Int Native()
        {
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