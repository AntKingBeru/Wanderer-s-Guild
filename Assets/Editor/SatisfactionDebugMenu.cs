// Play-Mode debug menu for satisfaction: log every mood with its reasons, push the first idle adventurer to Miserable or
// Happy, clear those pushes, and run the dawn tally now.
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class SatisfactionDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Satisfaction/";
        private static readonly List<MoodEntry> Entries = new();

        [MenuItem(MenuRoot + "Log Moods")]
        private static void Log()
        {
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                var manager = SatisfactionManager.Instance;
                var score = manager.Explain(adventurer, Entries);
                var reasons = string.Join(", ", Entries.Select(entry => $"{entry.Reason} {entry.Value:+0.#;-0.#}"));
                Debug.Log($"[Satisfaction] {adventurer.Name}: {score:0} {manager.LevelOf(adventurer)} " +
                          $"(needs appeal {manager.RequiredAppeal(adventurer):0.##}, morale +{adventurer.MoraleBonus:P0}) — {reasons}");
            }
        }

        [MenuItem(MenuRoot + "Make First Idle Miserable")]
        private static void MakeMiserable() => Push(-60f);

        [MenuItem(MenuRoot + "Make First Idle Happy")]
        private static void MakeHappy() => Push(40f);

        [MenuItem(MenuRoot + "Clear Debug Pushes")]
        private static void Clear()
        {
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
                SatisfactionManager.Instance.DebugRemember(adventurer, 0f, 0f);
        }

        [MenuItem(MenuRoot + "Run Dawn Check Now")]
        private static void Dawn() => SatisfactionManager.Instance.DebugDawn();

        // NOTE: A 5-day debug memory on the first idle adventurer (replaces any earlier push).
        private static void Push(float amount)
        {
            var adventurer = AdventurerManager.Instance.Adventurers.FirstOrDefault(a => a.Status == AdventurerStatus.Idle);
            if (adventurer == null)
            {
                Debug.Log("[Satisfaction] Nobody idle.");
                return;
            }
            SatisfactionManager.Instance.DebugRemember(adventurer, amount, 5f);
            Debug.Log($"[Satisfaction] {adventurer.Name} pushed {amount:+0;-0}.");
        }

        [MenuItem(MenuRoot + "Log Moods", true)]
        [MenuItem(MenuRoot + "Make First Idle Miserable", true)]
        [MenuItem(MenuRoot + "Make First Idle Happy", true)]
        [MenuItem(MenuRoot + "Clear Debug Pushes", true)]
        [MenuItem(MenuRoot + "Run Dawn Check Now", true)]
        private static bool CanUse() => EditorApplication.isPlaying && SatisfactionManager.HasInstance && AdventurerManager.HasInstance;
    }
}