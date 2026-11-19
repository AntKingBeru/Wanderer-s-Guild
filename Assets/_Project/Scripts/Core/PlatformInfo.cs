// What the current platform supports: only desktop players (and the Editor) control the window and monitors; the web build
// (itch.io) can't, and can't quit.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: UI and behavior only — where files live is PlatformStorage's call (the Editor always saves to disk).
    public static class PlatformInfo
    {
        // True in the web player, or in the Editor with GameConfig › Platform › Simulate Web In Editor.
        public static bool IsWeb =>
            Application.platform == RuntimePlatform.WebGLPlayer ||
            (Application.isEditor && GameConfig.HasInstance && GameConfig.Instance.Platform.SimulateWebInEditor);

        // Resolution, window mode, monitor, V-Sync and the FPS limit.
        // NOTE: Desktop only — Screen.mainWindowDisplayInfo / GetDisplayLayout / MoveMainWindowTo throw NotSupportedException
        // on every other player (the web's browser owns the canvas). Every window/monitor API call must check this first.
        public static bool HasWindowControl => !IsWeb && IsDesktopOrEditor;

        // Quitting to the desktop (a browser tab can't close itself).
        public static bool CanQuit => !IsWeb;

        private static bool IsDesktopOrEditor =>
            Application.isEditor ||
            Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.OSXPlayer or RuntimePlatform.LinuxPlayer;
    }
}