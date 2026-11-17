// What the current platform supports: the web build (itch.io) has no window, monitor or frame-rate control and can't quit.
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

        // Resolution, window mode, monitor, V-Sync and the FPS limit (the browser owns all of these on the web).
        public static bool HasWindowControl => !IsWeb;

        // Quitting to the desktop (a browser tab can't close itself).
        public static bool CanQuit => !IsWeb;
    }
}