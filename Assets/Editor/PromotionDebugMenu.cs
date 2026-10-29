// Play-Mode debug menu for promotions: give everyone rank progress, end every running exam now, log promotion status.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class PromotionDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Promotion/";

        [MenuItem(MenuRoot + "Give Everyone 5 Rank Progress")]
        private static void GiveProgress() => PromotionManager.Instance.GrantProgressToAll(5f);

        [MenuItem(MenuRoot + "Finish Exams Now")]
        private static void FinishExams() => PromotionManager.Instance.ResolveAllNow();

        [MenuItem(MenuRoot + "Log Promotion Status")]
        private static void LogStatus()
        {
            foreach (var a in AdventurerManager.Instance.Adventurers)
            {
                var check = PromotionManager.Instance.Check(a);
                var verdict = check.CanPromote ? $"can take Rank {check.Target} ({check.Fee}g)" : check.Reason;
                Debug.Log($"{a.Name} — rank {a.Rank}, progress {a.RankProgress:0.#}, level {a.Level}, {a.Status}: {verdict}");
            }
        }

        [MenuItem(MenuRoot + "Give Everyone 5 Rank Progress", true)]
        [MenuItem(MenuRoot + "Finish Exams Now", true)]
        [MenuItem(MenuRoot + "Log Promotion Status", true)]
        private static bool CanUse() => EditorApplication.isPlaying && PromotionManager.HasInstance && AdventurerManager.HasInstance;
    }
}