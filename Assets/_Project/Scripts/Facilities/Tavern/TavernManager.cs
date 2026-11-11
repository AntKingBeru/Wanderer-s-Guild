// Game-scene tavern: serves supper each evening — everyone at the guild pays and becomes Well fed (or skips it); the guild takes the money.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Holds no state: the buff lives on each adventurer (saved there).
    public class TavernManager : Singleton<TavernManager>
    {
        private TavernSettings _settings;

        protected override void OnSingletonAwake() => _settings = GameConfig.Instance.Tavern;

        private void OnEnable() => GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
        private void OnDisable() => GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);

        // The best finished tavern's effect and level (null = no tavern).
        public (TavernEffect effect, int level) Best
        {
            get
            {
                if (!FacilityManager.HasInstance) return (null, 0);

                TavernEffect best = null;
                var bestLevel = 0;
                foreach (var room in FacilityManager.Instance.Rooms)
                {
                    var effect = room.Data.Effect<TavernEffect>();
                    if (effect is null || room.Level <= bestLevel)
                        continue;

                    best = effect;
                    bestLevel = room.Level;
                }
                return (best, bestLevel);
            }
        }

        // Debug and testing: supper, now.
        public void ServeSupperNow() => ServeSupper();

        private void OnHourStarted(GameDateTime now)
        {
            if (now.Minute == 0 && now.Hour == _settings.SupperHour)
                ServeSupper();
        }

        // NOTE: Only adventurers at the guild (idle or signed up) take part: each is cleared, then served if they pay —
        // "Well fed lasts until the next supper". Those away (quest, exam) keep what they left with. One booking per evening.
        private void ServeSupper()
        {
            if (!AdventurerManager.HasInstance)
                return;

            var (tavern, level) = Best;
            var price = tavern ? tavern.MealPriceAt(level) : 0;
            var bonus = tavern ? tavern.PowerBonusAt(level) : 0f;

            int served = 0, skipped = 0;
            long gold = 0;
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.Status != AdventurerStatus.Idle && adventurer.Status != AdventurerStatus.SignedUp &&
                    adventurer.Status != AdventurerStatus.Injured)
                    continue;

                var before = adventurer.WellFedBonus;
                adventurer.SetWellFed(0f);

                if (tavern is not null)
                {
                    if (adventurer.TrySpendWealth(price))
                    {
                        adventurer.SetWellFed(bonus);
                        served++;
                        gold += price;
                    }
                    else
                    {
                        skipped++;
                    }
                }

                if (!Mathf.Approximately(before, adventurer.WellFedBonus))
                    GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
            }

            if (tavern is null)
                return;

            if (gold > 0 && GuildManager.HasInstance)
                GuildManager.Instance.Wallet.Add(gold, GoldReason.TavernMeals);
            GameEventsRelay.OnSupperServed.Invoke(served, skipped, gold);
        }
    }
}