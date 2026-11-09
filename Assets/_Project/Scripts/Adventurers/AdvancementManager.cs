// Game-scene owner of class advancement: eligibility, starting a trial (fee), resolving trials on return (class change,
// stats kept, gear re-fitted), unlocking advanced classes for arrivals, repairing trial state, and saving the unlocks.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Trials live on the adventurers (saved there); the unlocked classes
    // are this manager's own state (restored in Awake, before any arrival can ask).
    public class AdvancementManager : Singleton<AdvancementManager>, ISaveContributor
    {
        private readonly List<Adventurer> _due = new();
        private readonly HashSet<string> _unlocked = new();
        private AdvancementSettings _settings;

        private static long Now => RequestManager.NowMinutes;
        private static GuildRank GuildRank => GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GameConfig.Instance.NewGuild.StartingRank;
        private static long GuildGold => GuildManager.HasInstance ? GuildManager.Instance.Wallet.Gold : 0L;

        public IReadOnlyCollection<string> UnlockedClassIds => _unlocked;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Advancement;

            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data == null)
                return;
            foreach (var id in data.UnlockedClassIds)
            {
                if (!string.IsNullOrEmpty(id))
                    _unlocked.Add(id);
            }
        }

        private void OnEnable() => GameEventsRelay.OnTimeTick.AddListener(OnTimeTick);
        private void OnDisable() => GameEventsRelay.OnTimeTick.RemoveListener(OnTimeTick);

        // NOTE: Repairs inconsistent saves: In trial with no return time or class → Idle; trial data without the status → cleared.
        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);
            if (!AdventurerManager.HasInstance)
                return;

            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                var inTrial = adventurer.Status == AdventurerStatus.InTrial;
                var hasTrial = adventurer.TrialReturnsAtMinutes > 0 && !string.IsNullOrEmpty(adventurer.TrialClassId);

                switch (inTrial)
                {
                    case true when !hasTrial:
                        adventurer.EndTrial();
                        break;
                    case false when hasTrial:
                        adventurer.ClearTrial();
                        break;
                }
            }
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            base.OnDestroy();
        }

        // True once someone has advanced into this class (it may then arrive).
        public bool IsUnlocked(string classId) => !string.IsNullOrEmpty(classId) && _unlocked.Contains(classId);

        public AdvancementCheck Check(Adventurer adventurer) =>
            AdventurerManager.HasInstance
                ? AdvancementRules.Check(adventurer, GuildRank, GuildGold, GameConfig.Instance.Promotion, _settings,
                                         AdventurerManager.Instance.Classes)
                : AdvancementCheck.NotAtCap();

        // Starts a trial for one of the offered classes if still eligible (re-checked) and the guild pays.
        // False = nothing changed.
        public bool StartTrial(int adventurerId, string classId)
        {
            var adventurer = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(adventurerId) : null;
            if (adventurer == null)
                return false;

            var check = Check(adventurer);
            if (!check.CanAdvance || !Offers(check, classId))
                return false;
            if (check.Fee > 0 && !(GuildManager.HasInstance && GuildManager.Instance.Wallet.TrySpend(check.Fee, GoldReason.PromotionFee)))
                return false;

            adventurer.StartTrial(classId, Now + (long)_settings.TrialDays * GameCalendar.MinutesPerDay);
            GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
            return true;
        }

        // Debug and testing: every running trial ends now.
        public void FinishTrialsNow() => ResolveDue(long.MaxValue);

        public void WriteTo(GameSaveData data) => data.SetUnlockedClasses(_unlocked);

        private void OnTimeTick(GameDateTime now) => ResolveDue(now.TotalMinutes);

        // NOTE: Collected first, then resolved (resolving raises events that may touch the roster).
        private void ResolveDue(long nowMinutes)
        {
            if (!AdventurerManager.HasInstance)
                return;

            _due.Clear();
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.Status == AdventurerStatus.InTrial && nowMinutes >= adventurer.TrialReturnsAtMinutes)
                    _due.Add(adventurer);
            }
            foreach (var adventurer in _due)
                Resolve(adventurer);
            _due.Clear();
        }

        // NOTE: A class that no longer exists cancels the trial (back to Idle, no change). Pass: new class at level 1 with
        // stats kept, rank kept (progress reset), gear re-fitted (sell-back paid by the guild if it can), and the class unlocked
        // for arrivals the first time.
        private void Resolve(Adventurer adventurer)
        {
            if (!AdventurerManager.Instance.TryGetClass(adventurer.TrialClassId, out ClassData target))
            {
                Debug.LogWarning($"[AdvancementManager] {adventurer.Name}'s trial class '{adventurer.TrialClassId}' no longer exists; trial cancelled.", this);
                adventurer.EndTrial();
                GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
                return;
            }

            var passed = Random.value < AdvancementRules.TrialChance(adventurer, GameConfig.Instance.Quests);
            if (!passed)
            {
                adventurer.EndTrial();
                GameEventsRelay.OnClassTrialFailed.Invoke(adventurer, target);
                GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
                return;
            }

            var previous = adventurer.Class;
            var retained = AdvancementRules.RetainedAfter(adventurer, target);
            adventurer.EndTrial();
            adventurer.AdvanceTo(target, retained);

            var refund = ClassGearSwap.Apply(adventurer, target, AdventurerManager.Instance.Equipment,
                                             GameConfig.Instance.Equipment.SellBackFraction);
            PaySellBack(adventurer, refund);

            if (_settings.PassReputation != 0 && GuildManager.HasInstance)
                GuildManager.Instance.Reputation.Change(_settings.PassReputation, ReputationSource.AdventurerAchievement);

            GameEventsRelay.OnClassAdvanced.Invoke(adventurer, previous, target);
            GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);

            if (_unlocked.Add(target.Id))
                GameEventsRelay.OnClassUnlocked.Invoke(target);
        }

        private void PaySellBack(Adventurer adventurer, int refund)
        {
            if (refund <= 0)
                return;

            if (GuildManager.HasInstance && GuildManager.Instance.Wallet.TrySpend(refund, GoldReason.EquipmentSales))
                adventurer.AddWealth(refund);
            else
                Debug.Log($"[AdvancementManager] The guild couldn't pay {adventurer.Name}'s {refund}g sell-back; items removed without a refund.", this);
        }

        private static bool Offers(AdvancementCheck check, string classId)
        {
            return check.Options.Any(option => option.Id == classId);
        }
    }
}