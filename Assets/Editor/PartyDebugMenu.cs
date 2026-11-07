// Play-Mode debug menu for parties: form one from the first idle loners, log parties, disband everything.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class PartyDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Parties/";

        [MenuItem(MenuRoot + "Form Party From First 3 Idle Loners")]
        private static void Form()
        {
            List<Adventurer> picked = new();
            foreach (var a in AdventurerManager.Instance.Adventurers)
            {
                if (a.Status == AdventurerStatus.Idle && PartyManager.Instance.PartyOf(a.Id) == null)
                    picked.Add(a);
                if (picked.Count == 3)
                    break;
            }
            if (!PartyManager.Instance.FormNow(picked))
                Debug.Log("[Parties] Not enough idle adventurers without a party.");
        }

        [MenuItem(MenuRoot + "Log Parties")]
        private static void Log()
        {
            foreach (var p in PartyManager.Instance.Parties)
                Debug.Log($"{p.Name} #{p.Id}: members [{string.Join(", ", p.MemberIds)}], successes {p.SharedSuccesses}, fail streak {p.FailStreak}");
            Debug.Log($"[Parties] {PartyManager.Instance.Parties.Count} parties.");
        }

        [MenuItem(MenuRoot + "Disband All")]
        private static void DisbandAll()
        {
            foreach (var p in new List<GuildParty>(PartyManager.Instance.Parties))
                PartyManager.Instance.Disband(p.Id);
        }

        [MenuItem(MenuRoot + "Form Party From First 3 Idle Loners", true)]
        [MenuItem(MenuRoot + "Log Parties", true)]
        [MenuItem(MenuRoot + "Disband All", true)]
        private static bool CanUse() => EditorApplication.isPlaying && PartyManager.HasInstance && AdventurerManager.HasInstance;
    }
}