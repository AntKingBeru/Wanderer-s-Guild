// Play-Mode debug menu for the tavern: serve supper now, log who's Well fed.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class TavernDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Tavern/";

        [MenuItem(MenuRoot + "Serve Supper Now")]
        private static void ServeSupper()
        {
            if (TavernManager.Instance.Best.effect == null)
                Debug.Log("[Tavern] No finished tavern — supper just clears everyone's buff.");
            TavernManager.Instance.ServeSupperNow();
        }

        [MenuItem(MenuRoot + "Log Well Fed")]
        private static void LogWellFed()
        {
            foreach (var a in AdventurerManager.Instance.Adventurers)
                Debug.Log($"{a.Name} ({a.Status}, {a.Wealth}g): {(a.WellFedBonus > 0f ? $"Well fed +{a.WellFedBonus:P0}" : "not fed")}");
        }

        [MenuItem(MenuRoot + "Serve Supper Now", true)]
        [MenuItem(MenuRoot + "Log Well Fed", true)]
        private static bool CanUse() => EditorApplication.isPlaying && TavernManager.HasInstance && AdventurerManager.HasInstance;
    }
}