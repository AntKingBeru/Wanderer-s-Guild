// Game-scene owner of quest risks: applies injuries and deaths from each quest's outcome, and recovers the injured on time.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Holds no state: injuries live on the adventurers (saved there).
    // Applied during OnQuestOutcome — before QuestManager releases the members — so the injured stay Injured.
    public class InjuryManager : Singleton<InjuryManager>
    {
        private readonly List<Adventurer> _due = new();
        private InjurySettings _settings;

        protected override void OnSingletonAwake() => _settings = GameConfig.Instance.Injuries;

        private void OnEnable()
        {
            GameEventsRelay.OnQuestOutcome.AddListener(OnQuestOutcome);
            GameEventsRelay.OnTimeTick.AddListener(OnTimeTick);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnQuestOutcome.RemoveListener(OnQuestOutcome);
            GameEventsRelay.OnTimeTick.RemoveListener(OnTimeTick);
        }

        // NOTE: Repairs edited/inconsistent saves: Injured without an end time recovers; an injury on someone not Injured is
        // cleared. Anyone past their time recovers on the first tick.
        private void Start()
        {
            if (!AdventurerManager.HasInstance)
                return;

            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                var injuredStatus = adventurer.Status == AdventurerStatus.Injured;
                switch (injuredStatus)
                {
                    case true when adventurer.InjuredUntilMinutes <= 0:
                        adventurer.Recover();
                        break;
                    case false when adventurer.Injury != InjurySeverity.None:
                        adventurer.ClearInjury();
                        break;
                }
            }
        }

        // Debug and testing: injures someone now.
        public void Injure(Adventurer adventurer, InjurySeverity severity, int days) => ApplyInjury(adventurer, severity, days);

        // Debug and testing: everyone injured recovers now.
        public void HealEveryone()
        {
            if (!AdventurerManager.HasInstance)
                return;
            foreach (var adventurer in new List<Adventurer>(AdventurerManager.Instance.Adventurers).Where(adventurer => adventurer.Status == AdventurerStatus.Injured))
            {
                RecoverNow(adventurer);
            }
        }

        private void OnQuestOutcome(Quest quest, List<Adventurer> members, bool success, float powerRatio)
        {
            if (ScriptedQuests.IsSafe(quest.Id))
                return; // NOTE: A scripted quest (the tutorial's) never injures or kills.
            
            foreach (var result in QuestRisks.Roll(members, success, powerRatio, _settings))
            {
                if (result.Died)
                    Kill(result.Adventurer, quest);
                else
                    ApplyInjury(result.Adventurer, result.Injury, result.RecoveryDays);
            }
        }

        // NOTE: Due adventurers are collected first, then recovered (recovering raises events that may touch the roster).
        private void OnTimeTick(GameDateTime now)
        {
            if (!AdventurerManager.HasInstance)
                return;

            _due.Clear();
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.Status == AdventurerStatus.Injured && now.TotalMinutes >= adventurer.InjuredUntilMinutes)
                    _due.Add(adventurer);
            }
            foreach (var adventurer in _due)
                RecoverNow(adventurer);
        }

        private void ApplyInjury(Adventurer adventurer, InjurySeverity severity, int days)
        {
            if (adventurer == null || severity == InjurySeverity.None)
                return;

            var until = RequestManager.NowMinutes + (long)Mathf.Max(1, days) * GameCalendar.MinutesPerDay;
            adventurer.Injure(severity, until);

            GameEventsRelay.OnAdventurerInjured.Invoke(adventurer, severity, days);
            GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
        }

        private static void RecoverNow(Adventurer adventurer)
        {
            adventurer.Recover();
            GameEventsRelay.OnAdventurerRecovered.Invoke(adventurer);
            GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
        }

        private void Kill(Adventurer adventurer, Quest quest)
        {
            if (!AdventurerManager.HasInstance || !AdventurerManager.Instance.RemoveMember(adventurer.Id))
                return;

            if (_settings.DeathReputation != 0 && GuildManager.HasInstance)
                GuildManager.Instance.Reputation.Change(_settings.DeathReputation, ReputationSource.AdventurerDied);
            GameEventsRelay.OnAdventurerDied.Invoke(adventurer, quest);
        }
    }
}