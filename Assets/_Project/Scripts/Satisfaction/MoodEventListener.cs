// Turns game events into mood memories (Observer): meals, quest results and pay, injuries and infirmary care, exams and
// trials, parties forming or breaking up, and recalls.
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Only maps events → SatisfactionManager.Remember; the manager checks they're still on the roster (a member who
    // died in the same outcome is already gone).
    public sealed class MoodEventListener
    {
        private readonly SatisfactionManager _owner;
        private readonly SatisfactionSettings _settings;

        public MoodEventListener(SatisfactionManager owner, SatisfactionSettings settings)
        {
            _owner = owner;
            _settings = settings;
        }

        public void Subscribe()
        {
            GameEventsRelay.OnSupperOutcome.AddListener(OnSupperOutcome);
            GameEventsRelay.OnQuestOutcome.AddListener(OnQuestOutcome);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnAdventurerInjured.AddListener(OnInjured);
            GameEventsRelay.OnAdventurerRecovered.AddListener(OnRecovered);
            GameEventsRelay.OnPromotionExamEnded.AddListener(OnExamEnded);
            GameEventsRelay.OnClassAdvanced.AddListener(OnClassAdvanced);
            GameEventsRelay.OnClassTrialFailed.AddListener(OnTrialFailed);
            GameEventsRelay.OnPartyFormed.AddListener(OnPartyFormed);
            GameEventsRelay.OnPartyDisbanded.AddListener(OnPartyDisbanded);
        }

        public void Unsubscribe()
        {
            GameEventsRelay.OnSupperOutcome.RemoveListener(OnSupperOutcome);
            GameEventsRelay.OnQuestOutcome.RemoveListener(OnQuestOutcome);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnAdventurerInjured.RemoveListener(OnInjured);
            GameEventsRelay.OnAdventurerRecovered.RemoveListener(OnRecovered);
            GameEventsRelay.OnPromotionExamEnded.RemoveListener(OnExamEnded);
            GameEventsRelay.OnClassAdvanced.RemoveListener(OnClassAdvanced);
            GameEventsRelay.OnClassTrialFailed.RemoveListener(OnTrialFailed);
            GameEventsRelay.OnPartyFormed.RemoveListener(OnPartyFormed);
            GameEventsRelay.OnPartyDisbanded.RemoveListener(OnPartyDisbanded);
        }

        private void OnSupperOutcome(Adventurer adventurer, bool ate) =>
            _owner.Remember(adventurer, ate ? MoodReason.AteSupper : MoodReason.WentHungry);

        // NOTE: Pay compares the share they really got (after any potion cut) with the fair share.
        private void OnQuestOutcome(Quest quest, List<Adventurer> members, bool success, float _)
        {
            foreach (var member in members)
            {
                if (!success)
                {
                    _owner.Remember(member, MoodReason.QuestFailed);
                    continue;
                }

                _owner.Remember(member, MoodReason.QuestSucceeded);
                if (SatisfactionRules.TryPay(quest.EffectiveSharePercent, _settings, out var reason, out var scale))
                    _owner.Remember(member, reason, scale);
            }
        }

        private void OnQuestEnded(Quest quest, QuestEndReason reason)
        {
            if (reason != QuestEndReason.Recalled)
                return;
            foreach (var id in quest.MemberIds)
                _owner.Remember(Find(id), MoodReason.Recalled);
        }

        private void OnInjured(Adventurer adventurer, InjurySeverity severity, int _) =>
            _owner.Remember(adventurer, MoodReason.Injured, SatisfactionRules.InjuryScale(severity));

        // NOTE: Raised while they're still lying in the bed (the avatar re-plans afterward), so the infirmary check holds.
        private void OnRecovered(Adventurer adventurer)
        {
            if (InfirmaryCare.SpeedFor(adventurer.Id) > 1f)
                _owner.Remember(adventurer, MoodReason.InfirmaryCare);
        }

        private void OnExamEnded(Adventurer adventurer, GuildRank _, bool passed) =>
            _owner.Remember(adventurer, passed ? MoodReason.PromotionPassed : MoodReason.ExamFailed);

        private void OnClassAdvanced(Adventurer adventurer, ClassData _, ClassData __) =>
            _owner.Remember(adventurer, MoodReason.ClassAdvanced);

        private void OnTrialFailed(Adventurer adventurer, ClassData _) =>
            _owner.Remember(adventurer, MoodReason.TrialFailed);

        private void OnPartyFormed(GuildParty party)
        {
            foreach (var id in party.MemberIds)
                _owner.Remember(Find(id), MoodReason.PartyFormed);
        }

        private void OnPartyDisbanded(GuildParty party, PartyEndReason _)
        {
            foreach (var id in party.MemberIds)
                _owner.Remember(Find(id), MoodReason.PartyBrokeUp);
        }

        private static Adventurer Find(int id) => AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(id) : null;
    }
}