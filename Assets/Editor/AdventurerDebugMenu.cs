// Play-Mode debug menu for adventurers: open the roster, force an arrival, give everyone XP, log the roster.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class AdventurerDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Adventurers/";

        // NOTE: Stand-in until the roster has an in-world entry point; it will call the same GamePanelHost.Open.
        [MenuItem(MenuRoot + "Open Roster")]
        private static void OpenRoster()
        {
            var host = Object.FindAnyObjectByType<GamePanelHost>();
            if (!host || host.IsBusy || host.HasOpenPanel)
            {
                Debug.LogWarning("[Adventurers] No idle GamePanelHost in the scene (close any open panel first).");
                return;
            }
            host.Open(MenuPanelId.Roster);
        }

        [MenuItem(MenuRoot + "Force Arrival")]
        private static void ForceArrival()
        {
            if (!AdventurerManager.Instance.ForceArrival())
                Debug.Log("[Adventurers] Nobody arrived (guild full, or no species/class available).");
        }

        [MenuItem(MenuRoot + "Give Everyone 100,000 XP")]
        private static void GiveExperience()
        {
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
                AdventurerManager.Instance.GrantExperience(adventurer.Id, 100000);
        }

        [MenuItem(MenuRoot + "Log Roster")]
        private static void LogRoster()
        {
            foreach (var a in AdventurerManager.Instance.Adventurers)
            {
                Debug.Log($"#{a.Id} {a.Name} — {a.Species.DisplayName} {a.Class.DisplayName}, level {a.Level} " +
                          $"({a.Experience} XP, {a.LevelProgress:P0}), rank {a.Rank}, {a.Wealth} gold, {a.Status} — {a.Stats}");
            }
            Debug.Log($"[Adventurers] {AdventurerManager.Instance.Adventurers.Count}/{AdventurerManager.Instance.Capacity}.");
        }

        [MenuItem(MenuRoot + "Open Roster", true)]
        [MenuItem(MenuRoot + "Force Arrival", true)]
        [MenuItem(MenuRoot + "Give Everyone 1000 XP", true)]
        [MenuItem(MenuRoot + "Log Roster", true)]
        private static bool CanUse() => EditorApplication.isPlaying && AdventurerManager.HasInstance;
    }
}