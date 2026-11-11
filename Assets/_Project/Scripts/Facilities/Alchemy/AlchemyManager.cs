// Game-scene owner of potions: the stock (capacity from the alchemist), the morning brew prompt, brewing (paid), taking one
// at approval, clearing it when the alchemist is gone, and saving it.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". The stock restores in Awake (it only needs the tier count).
    public class AlchemyManager : Singleton<AlchemyManager>, ISaveContributor
    {
        private readonly PotionStock _stock = new();
        private AlchemySettings _settings;

        public PotionStock Stock => _stock;

        // Stock capacity from the best finished alchemist (0 = none).
        public int Capacity => Alchemy.Capacity;

        public int FreeSlots => Mathf.Max(0, Capacity - _stock.Total);

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Alchemy;

            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data != null)
                _stock.Restore(data.PotionStock, _settings.Tiers.Count);
        }

        private void OnEnable()
        {
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityRemoved);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityRemoved);
        }

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            base.OnDestroy();
        }

        // The brew cost of an order (no validation).
        public long CostOf(IReadOnlyList<(PotionKey key, int count)> order)
        {
            long cost = 0;
            foreach (var (key, count) in order)
            {
                if (Alchemy.TryGetTier(key.Tier, out var tier))
                    cost += (long)tier.BrewCost * Mathf.Max(0, count);
            }
            return cost;
        }

        // Why this order can't be brewed now, or null if it can (tiers brewable, counts &gt; 0, fits the free slots).
        public string CheckOrder(IReadOnlyList<(PotionKey key, int count)> order)
        {
            var total = 0;
            var maxTier = Alchemy.MaxTier;
            foreach (var (key, count) in order)
            {
                if (count <= 0)
                    return "Nothing to brew";
                if (key.IsNone || key.Tier > maxTier || !Alchemy.TryGetTier(key.Tier, out _))
                    return "Can't brew that tier";
                total += count;
            }

            if (total == 0) return "Nothing to brew";
            return total > FreeSlots ? "Not enough room" : null;
        }

        // Brews an order: validated as a whole, paid once, then stocked. False (nothing changes) if invalid or the
        // guild can't pay.
        public bool Brew(IReadOnlyList<(PotionKey key, int count)> order)
        {
            if (CheckOrder(order) != null)
                return false;

            var cost = CostOf(order);
            if (cost > 0 && !(GuildManager.HasInstance && GuildManager.Instance.Wallet.TrySpend(cost, GoldReason.PotionIngredients)))
                return false;

            var brewed = 0;
            foreach (var (key, count) in order)
            {
                _stock.Add(key, count);
                brewed += count;
            }

            GameEventsRelay.OnPotionsBrewed.Invoke(brewed, cost);
            GameEventsRelay.OnPotionStockChanged.Invoke();
            return true;
        }

        // Takes one potion for a quest (approval). False if none is in stock.
        public bool TryTake(PotionKey key)
        {
            if (!_stock.TryTake(key))
                return false;

            GameEventsRelay.OnPotionStockChanged.Invoke();
            return true;
        }

        // Debug and testing: the morning prompt, now (if there's room).
        public void RequestBrewNow() => RequestBrew();

        public void WriteTo(GameSaveData data) => data.SetPotionStock(_stock.ToSaved());

        // NOTE: Exactly the brew hour, once a day; only with an alchemist and free slots.
        private void OnHourStarted(GameDateTime now)
        {
            if (now.Minute == 0 && now.Hour == _settings.BrewHour)
                RequestBrew();
        }

        private void RequestBrew()
        {
            var free = FreeSlots;
            if (Capacity > 0 && free > 0)
                GameEventsRelay.OnBrewPromptRequested.Invoke(free);
        }

        // NOTE: No alchemist left → the stock goes with it. A lower capacity keeps the extra potions (no brewing until there's room).
        private void OnFacilityRemoved(PlacedFacility _)
        {
            if (Capacity > 0 || _stock.Total == 0)
                return;

            _stock.Clear();
            GameEventsRelay.OnPotionStockChanged.Invoke();
        }
    }
}