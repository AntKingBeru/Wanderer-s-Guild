// Writes the guild's notable events into the office log book: quest lifecycle and results, promotions, advancements, arrivals
// and departures.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Observer — only listens and writes (GuildLog stores, prunes and saves). Order 20: after QuestManager restores its
    // quests in Start, so their states are known before any update (a departure is a change INTO InProgress).
    [DefaultExecutionOrder(20)]
    public class GuildLogRecorder : MonoBehaviour
    {
        private readonly Dictionary<int, QuestState> _questStates = new();

        private void OnEnable()
        {
            GameEventsRelay.OnQuestPosted.AddListener(OnQuestPosted);
            GameEventsRelay.OnQuestPartyReady.AddListener(OnPartyReady);
            GameEventsRelay.OnQuestUpdated.AddListener(OnQuestUpdated);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnPromotionExamStarted.AddListener(OnExamStarted);
            GameEventsRelay.OnPromotionExamEnded.AddListener(OnExamEnded);
            GameEventsRelay.OnClassTrialStarted.AddListener(OnTrialStarted);
            GameEventsRelay.OnClassAdvanced.AddListener(OnClassAdvanced);
            GameEventsRelay.OnClassTrialFailed.AddListener(OnTrialFailed);
            GameEventsRelay.OnApplicantArrived.AddListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.AddListener(OnApplicantLeft);
            GameEventsRelay.OnAdventurerJoined.AddListener(OnJoined);
            GameEventsRelay.OnAdventurerDismissed.AddListener(OnDismissed);
            GameEventsRelay.OnAdventurerQuit.AddListener(OnQuit);
            GameEventsRelay.OnAdventurerDied.AddListener(OnDied);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnQuestPosted.RemoveListener(OnQuestPosted);
            GameEventsRelay.OnQuestPartyReady.RemoveListener(OnPartyReady);
            GameEventsRelay.OnQuestUpdated.RemoveListener(OnQuestUpdated);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnPromotionExamStarted.RemoveListener(OnExamStarted);
            GameEventsRelay.OnPromotionExamEnded.RemoveListener(OnExamEnded);
            GameEventsRelay.OnClassTrialStarted.RemoveListener(OnTrialStarted);
            GameEventsRelay.OnClassAdvanced.RemoveListener(OnClassAdvanced);
            GameEventsRelay.OnClassTrialFailed.RemoveListener(OnTrialFailed);
            GameEventsRelay.OnApplicantArrived.RemoveListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.RemoveListener(OnApplicantLeft);
            GameEventsRelay.OnAdventurerJoined.RemoveListener(OnJoined);
            GameEventsRelay.OnAdventurerDismissed.RemoveListener(OnDismissed);
            GameEventsRelay.OnAdventurerQuit.RemoveListener(OnQuit);
            GameEventsRelay.OnAdventurerDied.RemoveListener(OnDied);
        }

        private void Start()
        {
            if (!QuestManager.HasInstance)
                return;
            foreach (var quest in QuestManager.Instance.Quests)
                _questStates[quest.Id] = quest.State;
        }

        // ---------- Quests ----------

        private void OnQuestPosted(Quest quest)
        {
            _questStates[quest.Id] = quest.State;
            Write(LogCategory.Quest, LogTone.Normal, $"Posted {Title(quest)}");
        }

        private void OnPartyReady(Quest quest) =>
            Write(LogCategory.Quest, LogTone.Normal, $"{Title(quest)}: party ready for approval");

        // NOTE: Only a change into InProgress from a state seen before — a quest first seen now isn't guessed at.
        private void OnQuestUpdated(Quest quest)
        {
            var known = _questStates.TryGetValue(quest.Id, out var previous);
            _questStates[quest.Id] = quest.State;
            if (known && previous != QuestState.InProgress && quest.State == QuestState.InProgress)
                Write(LogCategory.Quest, LogTone.Normal, $"{Title(quest)}: party of {quest.MemberIds.Count} set out");
        }

        private void OnQuestEnded(Quest quest, QuestEndReason reason)
        {
            _questStates.Remove(quest.Id);
            var title = Title(quest);
            switch (reason)
            {
                case QuestEndReason.Succeeded:
                    var guildGold = QuestFormatter.GuildShare(quest, quest.MemberIds.Count);
                    Write(LogCategory.Quest, LogTone.Good, $"Succeeded: {title} (+{guildGold:N0}g)");
                    break;
                case QuestEndReason.Failed:
                    Write(LogCategory.Quest, LogTone.Bad, $"Failed: {title}");
                    break;
                case QuestEndReason.Expired:
                    Write(LogCategory.Quest, LogTone.Bad, $"Expired: {title}");
                    break;
                case QuestEndReason.Cancelled:
                    Write(LogCategory.Quest, LogTone.Normal, $"Cancelled: {title}");
                    break;
                case QuestEndReason.Recalled:
                    Write(LogCategory.Quest, LogTone.Normal, $"Recalled: {title}");
                    break;
            }
        }

        // ---------- Promotions and advancements ----------

        private void OnExamStarted(Adventurer adventurer, GuildRank target) =>
            Write(LogCategory.Promotion, LogTone.Normal, $"{adventurer.Name} began the Rank {target} exam");

        private void OnExamEnded(Adventurer adventurer, GuildRank target, bool passed) =>
            Write(LogCategory.Promotion, passed ? LogTone.Good : LogTone.Bad,
                  passed ? $"{adventurer.Name} passed the Rank {target} exam" : $"{adventurer.Name} failed the Rank {target} exam");

        private void OnTrialStarted(Adventurer adventurer, ClassData target) =>
            Write(LogCategory.Advancement, LogTone.Normal, $"{adventurer.Name} began the {target.DisplayName} trial");

        private void OnClassAdvanced(Adventurer adventurer, ClassData from, ClassData to) =>
            Write(LogCategory.Advancement, LogTone.Good, $"{adventurer.Name} became a {to.DisplayName}");

        private void OnTrialFailed(Adventurer adventurer, ClassData target) =>
            Write(LogCategory.Advancement, LogTone.Bad, $"{adventurer.Name} failed the {target.DisplayName} trial");

        // ---------- Arrivals and departures ----------

        private void OnApplicantArrived(Applicant applicant) =>
            Write(LogCategory.Arrival, LogTone.Normal, $"{applicant.Adventurer.Name} applied to join");

        private void OnJoined(Adventurer adventurer) =>
            Write(LogCategory.Arrival, LogTone.Good, $"{adventurer.Name} joined the guild");

        // NOTE: Accepted is the "joined" line; the office closing sends everyone away at once — one summary line per frame.
        private int _officeClosedFrame = -1;

        private void OnApplicantLeft(Applicant applicant, ApplicantEndReason reason)
        {
            var applicantName = applicant.Adventurer.Name;
            switch (reason)
            {
                case ApplicantEndReason.Declined:
                    Write(LogCategory.Departure, LogTone.Normal, $"Declined {applicantName}");
                    break;
                case ApplicantEndReason.Expired:
                    Write(LogCategory.Departure, LogTone.Bad, $"{applicantName} stopped waiting");
                    break;
                case ApplicantEndReason.OfficeClosed when _officeClosedFrame != Time.frameCount:
                    _officeClosedFrame = Time.frameCount;
                    Write(LogCategory.Departure, LogTone.Bad, "Applicants left: no office");
                    break;
            }
        }

        private void OnDismissed(Adventurer adventurer, int _) =>
            Write(LogCategory.Departure, LogTone.Normal, $"Dismissed {adventurer.Name}");

        private void OnQuit(Adventurer adventurer, int _) =>
            Write(LogCategory.Departure, LogTone.Bad, $"{adventurer.Name} quit the guild");

        private void OnDied(Adventurer adventurer, Quest quest) =>
            Write(LogCategory.Departure, LogTone.Bad,
                  quest != null ? $"{adventurer.Name} died on {Title(quest)}" : $"{adventurer.Name} died");

        private static string Title(Quest quest) => quest.Request != null ? quest.Request.DisplayName : "a quest";

        private static void Write(LogCategory category, LogTone tone, string text)
        {
            if (GuildLog.HasInstance)
                GuildLog.Instance.Add(category, tone, text);
        }
    }
}