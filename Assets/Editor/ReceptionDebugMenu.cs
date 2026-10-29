// Play-Mode debug menu for the reception desk: open the desk, or the applications screen directly.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class ReceptionDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Reception/";

        // NOTE: Stand-in until the guild scene has an in-world desk; it will call the same GamePanelHost.Open.
        [MenuItem(MenuRoot + "Open Reception Desk")]
        private static void OpenDesk() => Open(MenuPanelId.ReceptionDesk);

        [MenuItem(MenuRoot + "Open Applications")]
        private static void OpenApplications() => Open(MenuPanelId.Applications);

        private static void Open(MenuPanelId id)
        {
            var host = Object.FindAnyObjectByType<GamePanelHost>();
            if (!host || host.IsBusy || host.HasOpenPanel)
            {
                Debug.LogWarning("[Reception] No idle GamePanelHost in the scene (close any open panel first).");
                return;
            }
            host.Open(id);
        }

        [MenuItem(MenuRoot + "Open Reception Desk", true)]
        [MenuItem(MenuRoot + "Open Applications", true)]
        private static bool CanUse() => EditorApplication.isPlaying && Object.FindAnyObjectByType<GamePanelHost>();
    }
}