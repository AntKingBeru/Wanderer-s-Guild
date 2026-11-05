// Play-Mode debug menu for equipment: open the armory, restock or shop now, log stock and everyone's gear.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class EquipmentDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Equipment/";

        // NOTE: Stand-in for clicking the armory in the world (it calls the same GamePanelHost.Open).
        [MenuItem(MenuRoot + "Open Armory")]
        private static void OpenArmory()
        {
            var host = Object.FindAnyObjectByType<GamePanelHost>();
            if (!host || host.IsBusy || host.HasOpenPanel)
            {
                Debug.LogWarning("[Equipment] No idle GamePanelHost in the scene (close any open panel first).");
                return;
            }
            host.Open(MenuPanelId.Armory);
        }

        [MenuItem(MenuRoot + "Restock Now")]
        private static void Restock()
        {
            if (ArmoryManager.Instance.StockTier <= 0)
            {
                Debug.Log("[Equipment] No finished armory — nothing to restock.");
                return;
            }
            ArmoryManager.Instance.RestockNow();
        }

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

        [MenuItem(MenuRoot + "Log Stock")]
        private static void LogStock()
        {
            var armory = ArmoryManager.Instance;
            foreach (var item in armory.OnSale)
                Debug.Log($"{item.DisplayName} ({item.Id}): {armory.Stock.Units(item)}/{item.MaxStock}, {item.Price}g");
            Debug.Log($"[Equipment] Tier {armory.StockTier}; full restock would cost {armory.FullRestockCost}g.");
        }

        [MenuItem(MenuRoot + "Log Gear")]
        private static void LogGear()
        {
            foreach (var a in AdventurerManager.Instance.Adventurers)
                Debug.Log($"{a.Name} ({a.Class.DisplayName}, {a.Wealth}g): {AdventurerFormatter.Gear(a)} — base {a.BaseStats}, with gear {a.Stats}");
        }

        [MenuItem(MenuRoot + "Open Armory", true)]
        [MenuItem(MenuRoot + "Restock Now", true)]
        [MenuItem(MenuRoot + "Run Armory Shopping Now", true)]
        [MenuItem(MenuRoot + "Log Stock", true)]
        [MenuItem(MenuRoot + "Log Gear", true)]
        private static bool CanUse() => EditorApplication.isPlaying && ArmoryManager.HasInstance && AdventurerManager.HasInstance;
    }
}