// Play-Mode debug menu for class advancement: cap the first idle adventurer, start the first eligible trial, finish trials, log.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class AdvancementDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Advancement/";

        [MenuItem(MenuRoot + "Cap First Idle (max level + class rank cap)")]
        private static void CapFirst()
        {
            foreach (var a in AdventurerManager.Instance.Adventurers)
            {
                if (a.Status != AdventurerStatus.Idle || AdvancementRules.IsAtClassCap(a))
                    continue;

                AdventurerManager.Instance.GrantExperience(a.Id, 1_000_000);
                a.DebugSetRank(a.Class.MaxRank);
                GameEventsRelay.OnAdventurerChanged.Invoke(a);
                Debug.Log($"[Advancement] {a.Name}: Lv {a.Level}, Rank {a.Rank} ({a.Class.DisplayName}).");
                return;
            }
            Debug.Log("[Advancement] No idle adventurer below their class cap.");
        }

        [MenuItem(MenuRoot + "Start First Eligible Trial")]
        private static void StartFirst()
        {
            foreach (var a in AdventurerManager.Instance.Adventurers)
            {
                var check = AdvancementManager.Instance.Check(a);
                if (!check.CanAdvance)
                    continue;

                AdvancementManager.Instance.StartTrial(a.Id, check.Options[0].Id);
                Debug.Log($"[Advancement] {a.Name} started the {check.Options[0].DisplayName} trial (−{check.Fee}g).");
                return;
            }
            Debug.Log("[Advancement] Nobody can start a trial.");
        }

        [MenuItem(MenuRoot + "Finish Trials Now")]
        private static void Finish() => AdvancementManager.Instance.FinishTrialsNow();

        [MenuItem(MenuRoot + "Log Eligibility")]
        private static void Log()
        {
            foreach (var a in AdventurerManager.Instance.Adventurers)
            {
                var check = AdvancementManager.Instance.Check(a);
                var state = !check.IsAtClassCap ? "not at cap" : check.CanAdvance ? $"can advance ({check.Options.Count} options, {check.Fee}g)" : check.Reason;
                Debug.Log($"{a.Name} ({a.Class.DisplayName} Lv {a.Level}, Rank {a.Rank}): {state}");
            }
        }
        
        [MenuItem(MenuRoot + "Log Unlocked Classes")]
        private static void LogUnlocked() =>
            Debug.Log($"[Advancement] Unlocked: {string.Join(", ", AdvancementManager.Instance.UnlockedClassIds)}");

        [MenuItem(MenuRoot + "Cap First Idle (max level + class rank cap)", true)]
        [MenuItem(MenuRoot + "Start First Eligible Trial", true)]
        [MenuItem(MenuRoot + "Finish Trials Now", true)]
        [MenuItem(MenuRoot + "Log Eligibility", true)]
        [MenuItem(MenuRoot + "Log Unlocked Classes", true)]
        private static bool CanUse() => EditorApplication.isPlaying && AdvancementManager.HasInstance && AdventurerManager.HasInstance;
    }
}