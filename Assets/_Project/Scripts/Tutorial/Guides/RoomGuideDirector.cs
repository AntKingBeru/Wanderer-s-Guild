// Room guides after the start tutorial: announces each room once it can be built, keeps a goal line until it's built, then
// explains it — never blocking play.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Observer: re-checked on gold, rank and facility changes; idle until
    // the start tutorial is done. Order 150 so the first check (Start) sees the restored guild. Done guides are saved through
    // TutorialManager; "announced" isn't — after a load, guides whose room can already be built just show their goal line.
    [DefaultExecutionOrder(150)]
    public class RoomGuideDirector : Singleton<RoomGuideDirector>
    {
        private readonly HashSet<string> _announced = new();

        // The goal line under the HUD bar (null = none).
        public string GoalLine { get; private set; }

        // The room the active guide asks for while it isn't placed (null = none) — the hammer and its card glow.
        public FacilityData HighlightFacility { get; private set; }

        private static bool IsReady =>
            TutorialManager.HasInstance && TutorialManager.Instance.IsDone && FacilityManager.HasInstance && GuildManager.HasInstance;

        private void OnEnable()
        {
            GameEventsRelay.OnTutorialCompleted.AddListener(OnTutorialCompleted);
            GameEventsRelay.OnGoldChanged.AddListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.AddListener(OnRankUp);
            GameEventsRelay.OnFacilityPlaced.AddListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityCompleted.AddListener(OnFacilityCompleted);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnTutorialCompleted.RemoveListener(OnTutorialCompleted);
            GameEventsRelay.OnGoldChanged.RemoveListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.RemoveListener(OnRankUp);
            GameEventsRelay.OnFacilityPlaced.RemoveListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(OnFacilityCompleted);
        }

        // NOTE: Silent — a loaded guild doesn't get popups for what it could already do.
        private void Start() => Evaluate(announce: false);

        private void OnTutorialCompleted(bool _) => Evaluate(announce: true);
        private void OnGoldChanged(long _, long __) => Evaluate(announce: true);
        private void OnRankUp(GuildRank _) => Evaluate(announce: true);
        private void OnFacilityChanged(PlacedFacility _) => Evaluate(announce: true);

        // NOTE: Explained first, so the check after sees the guide done.
        private void OnFacilityCompleted(PlacedFacility room)
        {
            ExplainIfGuided(room);
            Evaluate(announce: true);
        }

        // ---------- Checking ----------

        // NOTE: Every guide is checked (so each can be announced), but only the first active one sets the goal line.
        private void Evaluate(bool announce)
        {
            if (!IsReady)
            {
                SetGoal(null, null);
                return;
            }

            string line = null;
            FacilityData highlight = null;
            foreach (var guide in GameConfig.Instance.Tutorial.RoomGuides)
            {
                if (!TryGetLine(guide, announce, out var guideLine, out var isPlaced) || line != null)
                    continue;

                line = guideLine;
                highlight = isPlaced ? null : guide.Facility;
            }

            SetGoal(line, highlight);
        }

        // NOTE: Active = announced, or its room placed. A finished room here (a load) completes the guide silently — during
        // play OnFacilityCompleted explains it first. Once announced, a dip in gold doesn't hide the goal again.
        private bool TryGetLine(RoomGuide guide, bool announce, out string line, out bool isPlaced)
        {
            line = null;
            isPlaced = false;
            if (guide.Facility == null || IsCompleted(guide))
                return false;

            var room = RoomGuideRules.FirstPlaced(guide.Facility);
            if (room is { IsUnderConstruction: false })
            {
                Complete(guide);
                return false;
            }

            if (room != null)
            {
                isPlaced = true;
                line = $"{guide.Facility.DisplayName}: being built";
                return true;
            }

            if (!_announced.Contains(guide.Id))
            {
                if (!RoomGuideRules.CanBuildNow(guide))
                    return false;

                _announced.Add(guide.Id);
                if (announce)
                    GameEventsRelay.OnRoomGuideMessage.Invoke($"New room: {guide.Facility.DisplayName}", guide.Intro);
            }

            line = guide.Goal;
            return true;
        }

        // ---------- Done ----------

        // NOTE: The first finished room of a guided facility (upgrades come later, after the guide is done).
        private void ExplainIfGuided(PlacedFacility room)
        {
            if (!IsReady || room == null)
                return;

            foreach (var guide in GameConfig.Instance.Tutorial.RoomGuides)
            {
                if (guide.Facility != room.Data || IsCompleted(guide))
                    continue;

                Complete(guide);
                GameEventsRelay.OnRoomGuideMessage.Invoke($"{guide.Facility.DisplayName} built!", guide.Built);
                return;
            }
        }

        private void Complete(RoomGuide guide)
        {
            _announced.Remove(guide.Id);
            TutorialManager.Instance.MarkGuideCompleted(guide.Id);
        }

        private static bool IsCompleted(RoomGuide guide) => TutorialManager.Instance.IsGuideCompleted(guide.Id);

        // NOTE: The event only fires when the line actually changes.
        private void SetGoal(string line, FacilityData highlight)
        {
            HighlightFacility = highlight;
            if (line == GoalLine)
                return;

            GoalLine = line;
            GameEventsRelay.OnRoomGuideGoalChanged.Invoke(line);
        }
    }
}