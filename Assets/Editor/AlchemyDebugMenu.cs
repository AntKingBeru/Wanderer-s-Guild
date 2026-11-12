// Play-Mode debug menu for potions: raise the brew prompt, fill the stock with Minor potions of the first category, log it.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class AlchemyDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Alchemy/";

        [MenuItem(MenuRoot + "Request Brew Prompt")]
        private static void Prompt() => AlchemyManager.Instance.RequestBrewNow();

        [MenuItem(MenuRoot + "Brew Minor Potions (first category)")]
        private static void BrewMinor()
        {
            var free = AlchemyManager.Instance.FreeSlots;
            if (free <= 0)
            {
                Debug.Log("[Alchemy] No free slots (or no alchemist).");
                return;
            }

            var first = (QuestCategory)System.Enum.GetValues(typeof(QuestCategory)).GetValue(0);
            List<(PotionKey, int)> order = new() { (new PotionKey(1, first), free) };
            if (!AlchemyManager.Instance.Brew(order))
                Debug.Log("[Alchemy] Couldn't brew (gold?).");
        }

        [MenuItem(MenuRoot + "Log Stock")]
        private static void Log()
        {
            foreach (var pair in AlchemyManager.Instance.Stock.Counts)
                Debug.Log($"Tier {pair.Key.Tier} {pair.Key.Category}: {pair.Value}");
            Debug.Log($"[Alchemy] {AlchemyManager.Instance.Stock.Total}/{AlchemyManager.Instance.Capacity}, max tier {Alchemy.MaxTier}.");
        }
        
        [MenuItem(MenuRoot + "Open Alchemist")]
        private static void Open()
        {
            var host = Object.FindAnyObjectByType<GamePanelHost>();
            if (host == null || host.IsBusy || host.HasOpenPanel)
            {
                Debug.LogWarning("[Alchemy] No idle GamePanelHost in the scene (close any open panel first).");
                return;
            }
            host.Open(MenuPanelId.Alchemy);
        }

        [MenuItem(MenuRoot + "Request Brew Prompt", true)]
        [MenuItem(MenuRoot + "Brew Minor Potions (first category)", true)]
        [MenuItem(MenuRoot + "Log Stock", true)]
        [MenuItem(MenuRoot + "Open Alchemist", true)]
        private static bool CanUse() => EditorApplication.isPlaying && AlchemyManager.HasInstance;
    }
}