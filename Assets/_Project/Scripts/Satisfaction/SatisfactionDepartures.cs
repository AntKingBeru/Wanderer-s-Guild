// The dawn departure check: counts Miserable dawns in a row, warns the day before, then a free adventurer quits (reputation
// loss; their party breaks up as "left").
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Tally during the dawn loop, Flush after it — quitting changes the roster. Busy adventurers keep counting and quit
    // on the first dawn they're free (Idle / Injured), so no quest, exam or trial loses a member. Nothing during the tutorial.
    public sealed class SatisfactionDepartures
    {
        private readonly SatisfactionSettings _settings;
        private readonly List<Adventurer> _leaving = new();

        public SatisfactionDepartures(SatisfactionSettings settings) => _settings = settings;

        private static bool TutorialRunning => TutorialManager.HasInstance && !TutorialManager.Instance.IsDone;

        public void Tally(Adventurer adventurer, AdventurerMood mood, SatisfactionLevel level)
        {
            mood.MiserableDays = level == SatisfactionLevel.Miserable ? mood.MiserableDays + 1 : 0;

            var leaveAfter = _settings.LeaveAfterDays;
            if (leaveAfter <= 0 || TutorialRunning)
                return;

            if (leaveAfter > 1 && mood.MiserableDays == leaveAfter - 1)
                GameEventsRelay.OnAdventurerLeavingSoon.Invoke(adventurer);
            else if (mood.MiserableDays >= leaveAfter && adventurer.Status is AdventurerStatus.Idle or AdventurerStatus.Injured)
                _leaving.Add(adventurer);
        }

        public void Flush()
        {
            foreach (var adventurer in _leaving)
                Quit(adventurer);
            _leaving.Clear();
        }

        // NOTE: OnAdventurerQuit fires before the removal, so the party ends as "left" (the removal then frees the bed and
        // takes the avatar away, as for any departure).
        private void Quit(Adventurer adventurer)
        {
            if (!AdventurerManager.HasInstance)
                return;

            var loss = Mathf.Max(0, _settings.LeaveReputation);
            GameEventsRelay.OnAdventurerQuit.Invoke(adventurer, loss);
            if (!AdventurerManager.Instance.RemoveMember(adventurer.Id))
                return;

            if (loss > 0 && GuildManager.HasInstance)
                GuildManager.Instance.Reputation.Change(-loss, ReputationSource.AdventurerDeparted);
        }
    }
}