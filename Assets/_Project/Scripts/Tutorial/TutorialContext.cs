// What the tutorial script needs from the game, in one place (Facade): screen and build-mode state, the guild objects, the
// guided rooms, the tutorial quest and the starting party.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class TutorialContext
    {
        private readonly GamePanelHost _host;

        public TutorialContext(GamePanelHost host, TutorialTargets targets)
        {
            _host = host;
            Targets = targets;
            Desk = targets.World(MenuPanelId.ReceptionDesk);
            QuestBoard = targets.World(MenuPanelId.Quests);
            Lounge = targets.World(MenuPanelId.Roster);
        }

        public TutorialTargets Targets { get; }
        public TutorialWorldTarget Desk { get; }
        public TutorialWorldTarget QuestBoard { get; }
        public TutorialWorldTarget Lounge { get; }

        // ---------- Screens, build mode ----------

        // True once this screen is open and done animating.
        public bool IsShowing(MenuPanelId id) => _host != null && _host.IsActive(id) && !_host.IsBusy;

        // True when no screen is open, opening or closing.
        public bool NothingOpen => _host != null && !_host.HasOpenPanel && !_host.IsBusy;

        public bool BuildModeActive => BuildModeController.HasInstance && BuildModeController.Instance.IsActive;

        // ---------- Guided rooms ----------

        // GameConfig › Tutorial › Placements, in build order.
        public IReadOnlyList<TutorialPlacement> Placements => GameConfig.Instance.Tutorial.Placements;

        // NOTE: Built = a room of that facility at that origin and rotation (a hand-placed room elsewhere doesn't count).
        public bool IsPlaced(TutorialPlacement placement) => FindRoom(placement) != null;

        // True once every one of these rooms is built and finished.
        public bool AreFinished(IEnumerable<TutorialPlacement> placements) =>
            placements.All(placement => FindRoom(placement) is { IsUnderConstruction: false });

        public int Capacity => AdventurerManager.HasInstance ? AdventurerManager.Instance.Capacity : 0;
        
        // NOTE: Only the tutorial's own rooms — anything else under construction keeps its timer. Rooms not built (or already
        // finished, like the halls) are ignored.
        public void FinishPlacementsNow(IEnumerable<TutorialPlacement> placements)
        {
            if (!FacilityManager.HasInstance)
                return;

            foreach (var placement in placements)
            {
                var room = FindRoom(placement);
                if (room != null)
                    FacilityManager.Instance.FinishNow(room);
            }
        }

        // ---------- The tutorial quest ----------

        // The tutorial's quest (−1 until it's posted).
        public int QuestId { get; private set; } = -1;

        public Quest Quest => QuestId >= 0 && QuestManager.HasInstance ? QuestManager.Instance.Find(QuestId) : null;
        public bool QuestPosted => Quest != null;
        public bool QuestDeparted => Quest is { State: QuestState.InProgress };

        // "Bran and Aelith" — the roster's given names.
        public string StartingPartyNames
        {
            get
            {
                if (!AdventurerManager.HasInstance)
                    return "Your adventurers";

                var names = AdventurerManager.Instance.Adventurers.Select(adventurer => AdventurerFormatter.GivenName(adventurer.Name)).ToList();
                return names.Count switch
                {
                    0 => "Your adventurers",
                    1 => names[0],
                    _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1]
                };
            }
        }

        // NOTE: The first quest posted while the tutorial runs is its quest (the gate allows no other) — held at once, so it
        // can't expire or come back on its own.
        public void Track(Quest quest)
        {
            if (QuestId >= 0 || quest == null)
                return;

            QuestId = quest.Id;
            ScriptedQuests.Hold(quest.Id);
        }

        public void SignUpStartingParty()
        {
            var quest = Quest;
            if (quest == null || !AdventurerManager.HasInstance)
            {
                Debug.LogWarning("[Tutorial] No tutorial quest to sign the starting party up for.");
                return;
            }

            QuestManager.Instance.ForceSignUp(quest.Id, new List<Adventurer>(AdventurerManager.Instance.Adventurers));
            if (quest.State != QuestState.Ready)
                Debug.LogWarning($"[Tutorial] The tutorial quest isn't full ({quest.MemberIds.Count}/{quest.Terms.PartySize}) — " +
                                 "check the request's Recommended Party Size against the starting party.");
        }

        // NOTE: A success with no injuries or deaths (ResolveScripted marks it safe and releases the hold) — the party comes
        // back now with their XP, rank progress and gold; the guild gets its share and guild points.
        public void ResolveQuest()
        {
            if (QuestId >= 0 && QuestManager.HasInstance && QuestManager.Instance.ResolveScripted(QuestId, success: true))
                return;

            Debug.LogWarning("[Tutorial] Couldn't resolve the tutorial quest (not posted, or not in progress).");
        }

        // The script's last step: the whole tutorial is done — requests, arrivals and saving resume, and it saves.
        // NOTE: Nothing else ends the guided tutorial; running out of steps doesn't.
        public void CompleteTutorial()
        {
            if (TutorialManager.HasInstance)
                TutorialManager.Instance.Complete();
        }

        // NOTE: When the tutorial stops early, its quest plays out normally instead of waiting forever.
        public void ReleaseQuest()
        {
            if (QuestId >= 0)
                ScriptedQuests.Release(QuestId);
        }

        private static PlacedFacility FindRoom(TutorialPlacement placement) =>
            !FacilityManager.HasInstance || placement.Facility == null
                ? null
                : FacilityManager.Instance.Rooms.FirstOrDefault(room => room.Data == placement.Facility &&
                                                                        room.Origin == placement.Origin &&
                                                                        room.Rotation == placement.Rotation);
    }
}