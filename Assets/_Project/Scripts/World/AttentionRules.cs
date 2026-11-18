// When a guild object asks for the player's attention (the bobbing "!"): the desk for new requests or a party awaiting approval,
// the office for waiting applicants.
using System.Linq;

namespace WanderersGuild
{
    // NOTE: Strategy by the screen the object opens (like InteractableLabels); add a case for any other object that should call
    // the player over, and list it in AttentionMarkerDirector.Tracked. Index loops — IReadOnlyList foreach would box.
    public static class AttentionRules
    {
        public static bool NeedsAttention(MenuPanelId opens) => opens switch
        {
            MenuPanelId.ReceptionDesk => HasUnseenRequest() || HasReadyQuest(),
            MenuPanelId.Recruits => RecruitmentManager.HasInstance && RecruitmentManager.Instance.Applicants.Count > 0,
            _ => false
        };

        // A request nobody has opened yet (its NEW tag).
        private static bool HasUnseenRequest()
        {
            if (!RequestManager.HasInstance)
                return false;

            var requests = RequestManager.Instance.Requests;
            return requests.Any(t => !t.IsSeen);
        }

        // A quest whose party filled and waits for the player's approval (Applications).
        private static bool HasReadyQuest()
        {
            if (!QuestManager.HasInstance)
                return false;

            var quests = QuestManager.Instance.Quests;
            return quests.Any(t => t.State == QuestState.Ready);
        }
    }
}