// Play-Mode debug menu for quests: open the board, post with defaults, run sign-ups, approve/decline, resolve, cancel, log.
using System.Linq;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class QuestDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Quests/";

        // NOTE: Stand-in until the guild scene has an in-world board; it will call the same GamePanelHost.Open.
        [MenuItem(MenuRoot + "Open Quest Board")]
        private static void OpenBoard()
        {
            var host = Object.FindAnyObjectByType<GamePanelHost>();
            if (!host || host.IsBusy || host.HasOpenPanel)
            {
                Debug.LogWarning("[Quests] No idle GamePanelHost in the scene (close any open panel first).");
                return;
            }
            host.Open(MenuPanelId.Quests);
        }

        // NOTE: Quick post with the default terms (the clipboard is the normal way). Refused when the quest board is full.
        [MenuItem(MenuRoot + "Post First Request (Defaults)")]
        private static void PostFirst()
        {
            if (RequestManager.Instance.Requests.Count == 0)
            {
                Debug.Log("[Quests] The request board is empty.");
                return;
            }

            var request = RequestManager.Instance.Requests[0];
            var terms = QuestManager.Instance.DefaultTerms(request.Data);
            Debug.Log(QuestManager.Instance.TryPost(request.InstanceId, terms, out Quest quest)
                ? $"[Quests] Posted #{quest.Id} \"{quest.Request.DisplayName}\": {terms.RewardSharePercent}% share, min rank {terms.MinRank}, party {terms.PartySize}."
                : $"[Quests] Not posted — the quest board is full ({QuestManager.Instance.Quests.Count}/{QuestManager.Instance.OpenSlots}).");
        }

        [MenuItem(MenuRoot + "Run Sign-ups Now")]
        private static void SignUps() => QuestManager.Instance.RunSignUpsNow();

        // NOTE: Stand-in for the Applications screen (14b).
        [MenuItem(MenuRoot + "Approve Ready Quests")]
        private static void ApproveReady()
        {
            foreach (var quest in new System.Collections.Generic.List<Quest>(QuestManager.Instance.Quests).Where(quest => quest.State == QuestState.Ready))
            {
                QuestManager.Instance.Approve(quest.Id);
            }
        }

        [MenuItem(MenuRoot + "Decline First Applicant")]
        private static void DeclineFirst()
        {
            foreach (var quest in QuestManager.Instance.Quests)
            {
                if (!quest.IsBeforeDeparture || quest.MemberIds.Count == 0)
                    continue;

                var id = quest.MemberIds[0];
                if (QuestManager.Instance.Decline(quest.Id, id))
                    Debug.Log($"[Quests] Declined adventurer #{id} from \"{quest.Request.DisplayName}\".");
                return;
            }
            Debug.Log("[Quests] No applicants to decline.");
        }

        [MenuItem(MenuRoot + "Resolve Active Quests Now")]
        private static void ResolveAll() => QuestManager.Instance.ResolveAllNow();

        [MenuItem(MenuRoot + "Cancel First Waiting Quest")]
        private static void CancelFirst()
        {
            foreach (var quest in QuestManager.Instance.Quests)
            {
                if (!quest.IsBeforeDeparture)
                    continue;
                QuestManager.Instance.Cancel(quest.Id);
                return;
            }
        }

        [MenuItem(MenuRoot + "Log Quests")]
        private static void LogQuests()
        {
            foreach (var quest in QuestManager.Instance.Quests)
            {
                Debug.Log($"#{quest.Id} \"{quest.Request.DisplayName}\" {quest.State} — {quest.MemberIds.Count}/{quest.Terms.PartySize} " +
                          $"(min {quest.Terms.MinRank}, {quest.Terms.RewardSharePercent}%, {quest.GoldPerMember}g each), " +
                          $"success {QuestManager.Instance.SuccessChance(quest):P0}");
            }
            Debug.Log($"[Quests] {QuestManager.Instance.Quests.Count}/{QuestManager.Instance.OpenSlots} slots used.");
        }

        [MenuItem(MenuRoot + "Open Quest Board", true)]
        [MenuItem(MenuRoot + "Post First Request (Defaults)", true)]
        [MenuItem(MenuRoot + "Run Sign-ups Now", true)]
        [MenuItem(MenuRoot + "Approve Ready Quests", true)]
        [MenuItem(MenuRoot + "Decline First Applicant", true)]
        [MenuItem(MenuRoot + "Resolve Active Quests Now", true)]
        [MenuItem(MenuRoot + "Cancel First Waiting Quest", true)]
        [MenuItem(MenuRoot + "Log Quests", true)]
        private static bool CanUse() => EditorApplication.isPlaying && QuestManager.HasInstance && RequestManager.HasInstance;
    }
}