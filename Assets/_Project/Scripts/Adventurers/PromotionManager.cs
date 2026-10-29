// Game-scene owner of rank promotion: grants rank progress on successful quests, starts exams (fee), resolves them on return.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Holds no state of its own: progress and exams live on the
    // adventurers and are saved with the roster.
    public class PromotionManager : Singleton<PromotionManager>
    {
        private PromotionSettings _settings;

        private static long Now => RequestManager.NowMinutes;
        private static GuildRank GuildRank => GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GameConfig.Instance.NewGuild.StartingRank;
        private static long GuildGold => GuildManager.HasInstance ? GuildManager.Instance.Wallet.Gold : 0L;

        protected override void OnSingletonAwake() => _settings = GameConfig.Instance.Promotion;

        private void OnEnable()
        {
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnTimeTick.AddListener(OnTimeTick);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnTimeTick.RemoveListener(OnTimeTick);
        }

        // NOTE: Repairs inconsistent saves: "in exam" with no return time → idle; a return time without the status → cleared.
        private void Start()
        {
            if (!AdventurerManager.HasInstance)
                return;

            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.Status == AdventurerStatus.InExam && adventurer.ExamReturnsAtMinutes <= 0)
                    adventurer.ClearExam(resetStatus: true);
                else if (adventurer.Status != AdventurerStatus.InExam && adventurer.ExamReturnsAtMinutes > 0)
                    adventurer.ClearExam(resetStatus: false);
            }
        }

        public PromotionCheck Check(Adventurer adventurer) => PromotionRules.Check(adventurer, GuildRank, GuildGold, _settings);

        // Starts the exam if still eligible (re-checked here) and the guild pays. False = nothing changed.
        public bool StartExam(int adventurerId)
        {
            var adventurer = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(adventurerId) : null;
            if (adventurer == null || !GuildManager.HasInstance)
                return false;

            var check = Check(adventurer);
            if (!check.CanPromote || !GuildManager.Instance.Wallet.TrySpend(check.Fee, GoldReason.PromotionFee))
                return false;

            adventurer.StartExam(Now + (long)_settings.ExamDays * GameCalendar.MinutesPerDay);
            GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
            GameEventsRelay.OnPromotionExamStarted.Invoke(adventurer, check.Target);
            return true;
        }

        // Debug and testing: every running exam ends now.
        public void ResolveAllNow()
        {
            if (!AdventurerManager.HasInstance)
                return;

            foreach (var adventurer in new List<Adventurer>(AdventurerManager.Instance.Adventurers).Where(adventurer => adventurer.Status == AdventurerStatus.InExam))
            {
                Resolve(adventurer);
            }
        }

        // Debug and testing: adds rank progress to everyone (capped).
        public void GrantProgressToAll(float amount)
        {
            if (!AdventurerManager.HasInstance)
                return;

            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.AddRankProgress(amount, _settings.ProgressRequired))
                    GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
            }
        }

        // NOTE: Progress depends on each member's rank at the moment the quest succeeds. Members no longer in the roster
        // are skipped.
        private void OnQuestEnded(Quest quest, QuestEndReason reason)
        {
            if (reason != QuestEndReason.Succeeded || !AdventurerManager.HasInstance)
                return;

            foreach (var id in quest.MemberIds)
            {
                var member = AdventurerManager.Instance.Find(id);
                if (member == null)
                    continue;

                var gain = _settings.ProgressFor(quest.Request.RecommendedRank, member.Rank);
                if (member.AddRankProgress(gain, _settings.ProgressRequired))
                    GameEventsRelay.OnAdventurerChanged.Invoke(member);
            }
        }

        // NOTE: Due exams are collected first, then resolved — never modifies state while iterating the roster.
        private void OnTimeTick(GameDateTime now)
        {
            if (!AdventurerManager.HasInstance)
                return;

            List<Adventurer> due = null;
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.Status == AdventurerStatus.InExam && now.TotalMinutes >= adventurer.ExamReturnsAtMinutes)
                    (due ??= new List<Adventurer>()).Add(adventurer);
            }

            if (due == null)
                return;
            foreach (var adventurer in due)
                Resolve(adventurer);
        }

        private void Resolve(Adventurer adventurer)
        {
            var target = adventurer.Rank < GuildRank.National ? adventurer.Rank + 1 : GuildRank.National;
            var passed = adventurer.Rank < GuildRank.National &&
                          Random.value < PromotionRules.ExamChance(adventurer, target, GameConfig.Instance.Quests);

            if (passed)
            {
                adventurer.PassExam(target);
                if (_settings.PassReputation != 0 && GuildManager.HasInstance)
                    GuildManager.Instance.Reputation.Change(_settings.PassReputation, ReputationSource.AdventurerAchievement);
            }
            else
            {
                adventurer.FailExam();
            }

            GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
            GameEventsRelay.OnPromotionExamEnded.Invoke(adventurer, target, passed);
        }
    }
}