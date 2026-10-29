// Play-Mode debug menu for the guild state: add/remove gold and reputation, add rank points (tests the HUD and rank-ups).
using UnityEditor;
using WanderersGuild;

namespace Editor
{
    public static class GuildDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/";

        [MenuItem(MenuRoot + "Gold +100")] private static void AddGold() => Guild.Wallet.Add(100, GoldReason.Debug);
        [MenuItem(MenuRoot + "Gold -100")] private static void SpendGold() => Guild.Wallet.TrySpend(100, GoldReason.Debug);
        [MenuItem(MenuRoot + "Reputation +10")] private static void AddReputation() => Guild.Reputation.Change(10, ReputationSource.Debug);
        [MenuItem(MenuRoot + "Reputation -10")] private static void RemoveReputation() => Guild.Reputation.Change(-10, ReputationSource.Debug);
        [MenuItem(MenuRoot + "Rank Points +50")] private static void AddRankPoints() => Guild.Rank.AddPoints(50);
        [MenuItem(MenuRoot + "Rank Points +1000")] private static void AddManyRankPoints() => Guild.Rank.AddPoints(1000);

        // NOTE: Every item above is only enabled in Play Mode with a GuildManager in the scene.
        [MenuItem(MenuRoot + "Gold +100", true)]
        [MenuItem(MenuRoot + "Gold -100", true)]
        [MenuItem(MenuRoot + "Reputation +10", true)]
        [MenuItem(MenuRoot + "Reputation -10", true)]
        [MenuItem(MenuRoot + "Rank Points +50", true)]
        [MenuItem(MenuRoot + "Rank Points +1000", true)]
        private static bool CanUse() => EditorApplication.isPlaying && GuildManager.HasInstance;

        private static GuildManager Guild => GuildManager.Instance;
    }
}