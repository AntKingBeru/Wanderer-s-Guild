// Play-Mode debug menu for injuries: injure the first idle adventurer, heal everyone, log injuries.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class InjuryDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Injuries/";

        [MenuItem(MenuRoot + "Injure First Idle (Serious, 3d)")]
        private static void InjureFirst()
        {
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.Status != AdventurerStatus.Idle)
                    continue;
                InjuryManager.Instance.Injure(adventurer, InjurySeverity.Serious, 3);
                return;
            }
            Debug.Log("[Injuries] Nobody idle to injure.");
        }

        [MenuItem(MenuRoot + "Heal Everyone")]
        private static void Heal() => InjuryManager.Instance.HealEveryone();

        [MenuItem(MenuRoot + "Log Injuries")]
        private static void Log()
        {
            var now = RequestManager.NowMinutes;
            foreach (var a in AdventurerManager.Instance.Adventurers)
            {
                if (a.Status == AdventurerStatus.Injured)
                    Debug.Log($"{a.Name}: {a.Injury}, back in {RequestFormatter.TimeLeft(System.Math.Max(0, a.InjuredUntilMinutes - now))}");
            }
        }

        [MenuItem(MenuRoot + "Injure First Idle (Serious, 3d)", true)]
        [MenuItem(MenuRoot + "Heal Everyone", true)]
        [MenuItem(MenuRoot + "Log Injuries", true)]
        private static bool CanUse() => EditorApplication.isPlaying && InjuryManager.HasInstance && AdventurerManager.HasInstance;
    }
}