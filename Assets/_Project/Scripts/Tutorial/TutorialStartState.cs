// Builds the state the start tutorial ends in (used when it's skipped): its rooms, finished, and its quest completed by the
// starting adventurers.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Builder — rooms first (the halls connect, the bedrooms house the starting party), then the quest: posted → the
    // starting adventurers sign up → depart → a success with no injuries (gold share, XP, rank progress, guild points). A
    // missing piece is logged and skipped; whatever can still be built is.
    public static class TutorialStartState
    {
        public static void Apply()
        {
            BuildRooms();
            CompleteQuest();
        }

        // NOTE: Through the normal rules (cost, connectivity, limits) — finished at once (build time × 0).
        private static void BuildRooms()
        {
            if (!FacilityManager.HasInstance)
                return;

            var facilities = FacilityManager.Instance;
            var placements = GameConfig.Instance.Tutorial.Placements;
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                if (placement.Facility == null)
                {
                    Debug.LogWarning($"[Tutorial] Placement {i} has no facility; skipped.");
                    continue;
                }

                if (facilities.TryPlace(placement.Facility, placement.Origin, placement.Rotation, buildTimeScale: 0f) != null)
                    continue;

                var reason = facilities.CanPlace(placement.Facility, placement.Origin, placement.Rotation).Reason;
                Debug.LogWarning($"[Tutorial] Couldn't build placement {i} ({placement.Facility.DisplayName} at " +
                                 $"{placement.Origin}, rotation {placement.Rotation}): {reason}");
            }
        }

        private static void CompleteQuest()
        {
            if (!RequestManager.HasInstance || !QuestManager.HasInstance || !AdventurerManager.HasInstance)
                return;

            var request = RequestManager.Instance.TutorialRequest;
            if (request == null)
            {
                Debug.LogWarning("[Tutorial] The tutorial request isn't on the board; no tutorial quest.");
                return;
            }

            var quests = QuestManager.Instance;
            if (!quests.TryPost(request.InstanceId, quests.DefaultTerms(request.Data), out var quest))
            {
                Debug.LogWarning("[Tutorial] Couldn't post the tutorial quest.");
                return;
            }

            quests.ForceSignUp(quest.Id, new List<Adventurer>(AdventurerManager.Instance.Adventurers));
            if (!quests.Approve(quest.Id))
            {
                Debug.LogWarning($"[Tutorial] The tutorial quest isn't full ({quest.MemberIds.Count}/{quest.Terms.PartySize}) — " +
                                 "check the request's Recommended Party Size against the starting party. It stays posted.");
                return;
            }

            quests.ResolveScripted(quest.Id, success: true);
        }
    }
}