// Play-Mode debug menu for equipment: run the armory's morning shopping now, log everyone's gear.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class EquipmentDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Equipment/";

        [MenuItem(MenuRoot + "Run Armory Shopping Now")]
        private static void RunShopping()
        {
            if (ArmoryManager.Instance.StockTier <= 0)
            {
                Debug.Log("[Equipment] No finished armory — nothing is for sale.");
                return;
            }
            ArmoryManager.Instance.RunShoppingNow();
        }

        [MenuItem(MenuRoot + "Log Gear")]
        private static void LogGear()
        {
            foreach (var a in AdventurerManager.Instance.Adventurers)
                Debug.Log($"{a.Name} ({a.Class.DisplayName}, {a.Wealth}g): {AdventurerFormatter.Gear(a)} — base {a.BaseStats}, with gear {a.Stats}");
            Debug.Log($"[Equipment] Armory stock tier {ArmoryManager.Instance.StockTier}.");
        }

        [MenuItem(MenuRoot + "Run Armory Shopping Now", true)]
        [MenuItem(MenuRoot + "Log Gear", true)]
        private static bool CanUse() => EditorApplication.isPlaying && ArmoryManager.HasInstance && AdventurerManager.HasInstance;
    }
}