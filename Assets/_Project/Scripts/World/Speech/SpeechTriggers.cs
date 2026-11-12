// Turns game events into speech requests: quest departures and results, coming back, level-ups, injuries, supper remarks
// and hourly chatter.
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace WanderersGuild
{
    // NOTE: Observer — it only asks to say things (say = adventurer id, topic, delay in in-game minutes); the queue and the
    // director decide when, and whether, they show.
    public sealed class SpeechTriggers
    {
        private readonly Action<int, SpeechTopic, int> _say;
        private readonly HashSet<int> _departedQuests = new();

        public SpeechTriggers(Action<int, SpeechTopic, int> say) => _say = say;

        private static SpeechSettings Settings => GameConfig.Instance.Speech;

        public void Bind()
        {
            GameEventsRelay.OnQuestUpdated.AddListener(OnQuestUpdated);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnQuestOutcome.AddListener(OnQuestOutcome);
            GameEventsRelay.OnAdventurerReturned.AddListener(OnRecalledBack);
            GameEventsRelay.OnPromotionExamEnded.AddListener(OnExamEnded);
            GameEventsRelay.OnClassAdvanced.AddListener(OnClassAdvanced);
            GameEventsRelay.OnClassTrialFailed.AddListener(OnClassTrialFailed);
            GameEventsRelay.OnAdventurerLeveledUp.AddListener(OnLeveledUp);
            GameEventsRelay.OnAdventurerInjured.AddListener(OnInjured);
            GameEventsRelay.OnSupperServed.AddListener(OnSupperServed);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
        }

        public void Unbind()
        {
            GameEventsRelay.OnQuestUpdated.RemoveListener(OnQuestUpdated);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnQuestOutcome.RemoveListener(OnQuestOutcome);
            GameEventsRelay.OnAdventurerReturned.RemoveListener(OnRecalledBack);
            GameEventsRelay.OnPromotionExamEnded.RemoveListener(OnExamEnded);
            GameEventsRelay.OnClassAdvanced.RemoveListener(OnClassAdvanced);
            GameEventsRelay.OnClassTrialFailed.RemoveListener(OnClassTrialFailed);
            GameEventsRelay.OnAdventurerLeveledUp.RemoveListener(OnLeveledUp);
            GameEventsRelay.OnAdventurerInjured.RemoveListener(OnInjured);
            GameEventsRelay.OnSupperServed.RemoveListener(OnSupperServed);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            _departedQuests.Clear();
        }

        // NOTE: OnQuestUpdated fires many times per quest — only the first time it's in progress do the members (now
        // walking out) announce it. Quests restored in progress have no avatars, so their requests are simply dropped.
        private void OnQuestUpdated(Quest quest)
        {
            if (quest == null || quest.State != QuestState.InProgress || !QuestManager.HasInstance || !_departedQuests.Add(quest.Id))
                return;

            foreach (var member in QuestManager.Instance.MembersOf(quest))
                _say(member.Id, SpeechTopic.Departing, 0);
        }

        private void OnQuestEnded(Quest quest, QuestEndReason _)
        {
            if (quest != null)
                _departedQuests.Remove(quest.Id);
        }

        // NOTE: Fires while the party is still away — the lines wait for them at the entrance.
        private void OnQuestOutcome(Quest _, List<Adventurer> members, bool success, float __)
        {
            if (members == null)
                return;

            var topic = success ? SpeechTopic.QuestSucceeded : SpeechTopic.QuestFailed;
            foreach (var member in members)
                SayTo(member, topic);
        }

        private void OnRecalledBack(Adventurer adventurer) => SayTo(adventurer, SpeechTopic.CameBack);
        private void OnExamEnded(Adventurer adventurer, GuildRank _, bool __) => SayTo(adventurer, SpeechTopic.CameBack);
        private void OnClassAdvanced(Adventurer adventurer, ClassData _, ClassData __) => SayTo(adventurer, SpeechTopic.CameBack);
        private void OnClassTrialFailed(Adventurer adventurer, ClassData _) => SayTo(adventurer, SpeechTopic.CameBack);
        private void OnLeveledUp(Adventurer adventurer) => SayTo(adventurer, SpeechTopic.LeveledUp);
        private void OnInjured(Adventurer adventurer, InjurySeverity _, int __) => SayTo(adventurer, SpeechTopic.Injured);

        // NOTE: Only some of those who ate speak, spread over the next minutes so they don't all talk at once.
        private void OnSupperServed(int served, int _, long __)
        {
            if (served <= 0 || !AvatarManager.HasInstance || !AdventurerManager.HasInstance)
                return;

            var settings = Settings;
            foreach (var avatar in AvatarManager.Instance.Avatars.Values)
            {
                if (avatar == null || avatar.IsLeaving || Random.value >= settings.WellFedChance)
                    continue;

                var adventurer = AdventurerManager.Instance.Find(avatar.AdventurerId);
                if (adventurer is { IsWellFed: true })
                    _say(avatar.AdventurerId, SpeechTopic.WellFed, Random.Range(0, settings.SupperSpreadMinutes + 1));
            }
        }

        // NOTE: At a random minute of the hour; the director turns Chatter into the activity's kind when it shows.
        private void OnHourStarted(GameDateTime _)
        {
            if (!AvatarManager.HasInstance)
                return;

            var chance = Settings.ChatterChance;
            foreach (var avatar in AvatarManager.Instance.Avatars.Values)
            {
                if (avatar != null && !avatar.IsLeaving && Random.value < chance)
                    _say(avatar.AdventurerId, SpeechTopic.Chatter, Random.Range(0, 60));
            }
        }

        private void SayTo(Adventurer adventurer, SpeechTopic topic)
        {
            if (adventurer != null)
                _say(adventurer.Id, topic, 0);
        }
    }
}