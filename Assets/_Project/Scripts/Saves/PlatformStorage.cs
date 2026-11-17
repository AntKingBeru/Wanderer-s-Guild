// Where saves and settings live: persistentDataPath on desktop and in the Editor; on the web, a fixed folder in the
// browser's IndexedDB that's flushed after every change, so reloads and new itch.io uploads keep it.
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace WanderersGuild
{
    // NOTE: Unity's web player mounts IndexedDB at /idbfs and loads it before the game starts, but its default
    // persistentDataPath is /idbfs/<hash of the page URL> — and itch.io serves every upload from a new URL. Files are
    // written to memory first; Flush copies them to IndexedDB (Plugins/WebGL/WebStorage.jslib).
    public static class PlatformStorage
    {
        private const string WebMount = "/idbfs";

        // Only the real web player — the Editor always uses the disk, even when simulating the web UI.
        public static bool UsesBrowserStorage => Application.platform == RuntimePlatform.WebGLPlayer;

        // The folder saves and settings go in.
        public static string Root => UsesBrowserStorage
            ? $"{WebMount}/{GameConfig.Instance.Platform.WebStorageFolder}"
            : Application.persistentDataPath;

        // NOTE: File.Replace isn't dependable on the web's file system; writers fall back to delete + move there.
        public static bool SupportsReplace => !UsesBrowserStorage;

        // Pushes everything written or deleted since the last flush to IndexedDB (no-op off the web; overlapping calls are
        // coalesced in the plugin).
        public static void Flush()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WG_SyncFileSystem();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WG_SyncFileSystem();
#endif
    }
}