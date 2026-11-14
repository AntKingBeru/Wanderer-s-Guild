// Quests under a script (the tutorial): held ones never expire or come back on their own; safe ones roll no injuries or deaths.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Registry by quest id — QuestManager and InjuryManager consult it; QuestManager forgets a quest when it ends.
    public static class ScriptedQuests
    {
        private static readonly HashSet<int> Held = new();
        private static readonly HashSet<int> Safe = new();

        public static void Hold(int questId) => Held.Add(questId);
        public static void Release(int questId) => Held.Remove(questId);
        public static bool IsHeld(int questId) => Held.Contains(questId);

        public static void MarkSafe(int questId) => Safe.Add(questId);
        public static bool IsSafe(int questId) => Safe.Contains(questId);

        public static void Forget(int questId)
        {
            Held.Remove(questId);
            Safe.Remove(questId);
        }

        public static void Clear()
        {
            Held.Clear();
            Safe.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Clear();
    }
}