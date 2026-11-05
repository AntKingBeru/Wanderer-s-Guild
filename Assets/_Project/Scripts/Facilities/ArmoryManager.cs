// Game-scene armory: stock restocked each dawn (wholesale, as much as the guild can afford), morning shopping from stock, save.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". The stock restores in Awake (it only needs the item lookup);
    // eligibility (tier, guild rank) is applied when restocking and shopping.
    public class ArmoryManager : Singleton<ArmoryManager>, ISaveContributor
    {
        private static readonly EquipmentSlot[] ShoppingOrder = { EquipmentSlot.Weapon, EquipmentSlot.Offhand, EquipmentSlot.Armor };

        private readonly ArmoryStock _stock = new();
        private readonly List<EquipmentData> _onSale = new();
        private readonly List<EquipmentData> _available = new();
        private EquipmentSettings _settings;
        private IReadOnlyList<EquipmentData> _items = System.Array.Empty<EquipmentData>();

        public ArmoryStock Stock => _stock;

        // Stock tier from every finished room's ArmoryEffect (0 = no armory).
        public int StockTier
        {
            get
            {
                if (!FacilityManager.HasInstance)
                    return 0;

                var tier = 0;
                foreach (var room in FacilityManager.Instance.Rooms)
                {
                    var effect = room.Data.Effect<ArmoryEffect>();
                    if (effect)
                        tier = Mathf.Max(tier, effect.TierAt(room.Level));
                }
                return tier;
            }
        }

        // Items the armory sells right now (tier + guild rank; starter items never), sold out or not.
        public IReadOnlyList<EquipmentData> OnSale
        {
            get
            {
                RefreshOnSale();
                return _onSale;
            }
        }

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Equipment;
            if (_settings.Database)
                _items = _settings.Database.Items;

            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data != null && _settings.Database)
                _stock.Restore(data.ArmoryStock, _settings.Database.BuildLookup());
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

        // Debug and testing: the dawn restock, now.
        public void RestockNow() => Restock();

        // Debug and testing: the morning shopping round, now.
        public void RunShoppingNow() => RunShopping();

        // What topping everything up would cost now (gold unlimited).
        public long FullRestockCost => ArmoryRestockPlan.FullCost(OnSale, _stock, _settings.WholesaleFraction);

        public void WriteTo(GameSaveData data) => data.SetArmoryStock(_stock.ToSaved());

        private void OnHourStarted(GameDateTime now)
        {
            if (now.Minute != 0)
                return;
            if (now.Hour == GameConfig.Instance.Time.DawnHour)
                Restock();
            if (now.Hour == _settings.ShoppingHour)
                RunShopping();
        }

        // NOTE: Plan against the current gold, then book once — if TrySpend refuses (gold changed), nothing is added. The
        // reputation hit only applies when something was missing and nothing could be bought.
        private void Restock()
        {
            RefreshOnSale();
            if (_onSale.Count == 0)
                return;

            var gold = GuildManager.HasInstance ? GuildManager.Instance.Wallet.Gold : 0;
            var plan = ArmoryRestockPlan.Build(_onSale, _stock, gold, _settings.WholesaleFraction);
            if (plan.Missing == 0)
                return;

            if (plan.Bought > 0 && GuildManager.HasInstance && GuildManager.Instance.Wallet.TrySpend(plan.Cost, GoldReason.ArmoryRestock))
            {
                foreach (var (key, value) in plan.UnitsByItem)
                    _stock.Add(key, value);

                GameEventsRelay.OnArmoryRestocked.Invoke(plan.Bought, plan.Cost, plan.Missing);
                GameEventsRelay.OnArmoryStockChanged.Invoke();
                return;
            }

            if (_settings.RestockFailReputation != 0 && GuildManager.HasInstance)
                GuildManager.Instance.Reputation.Change(_settings.RestockFailReputation, ReputationSource.ArmoryRestockFailed);
            GameEventsRelay.OnArmoryRestocked.Invoke(0, 0, plan.Missing);
        }

        // NOTE: Only idle adventurers shop, only from items with units left; each purchase takes a unit (and drops the item
        // from the candidates at 0). The budget is re-read before each slot.
        private void RunShopping()
        {
            if (!AdventurerManager.HasInstance)
                return;

            RefreshOnSale();
            _available.Clear();
            foreach (var item in _onSale.Where(item => _stock.Units(item) > 0))
            {
                _available.Add(item);
            }
            if (_available.Count == 0)
                return;

            var quests = GameConfig.Instance.Quests;
            var sold = 0;
            long sales = 0, buyBacks = 0;

            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.Status != AdventurerStatus.Idle)
                    continue;

                var changed = false;
                foreach (var slot in ShoppingOrder)
                {
                    var budget = (int)(adventurer.Wealth * _settings.SpendFraction);
                    var item = EquipmentRules.BestUpgrade(adventurer, slot, _available, budget, quests);
                    if (!item || !adventurer.TrySpendWealth(item.Price))
                        continue;
                    if (!_stock.TryTake(item))
                    {
                        adventurer.AddWealth(item.Price);
                        continue;
                    }
                    if (_stock.Units(item) == 0)
                        _available.Remove(item);

                    buyBacks += SellBack(adventurer, slot);
                    if (item.TwoHanded) buyBacks += SellBack(adventurer, EquipmentSlot.Offhand);
                    adventurer.Equipment.Set(slot, item);

                    sold++;
                    sales += item.Price;
                    changed = true;
                    GameEventsRelay.OnEquipmentBought.Invoke(adventurer, item);
                }

                if (changed)
                    GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
            }

            if (sold == 0)
                return;
            Book(sales - buyBacks, sold);
            GameEventsRelay.OnArmoryStockChanged.Invoke();
        }

        // NOTE: On sale = sold in the armory at all (not starter-only), armory tier reached, and the guild's rank reached.
        private void RefreshOnSale()
        {
            _onSale.Clear();
            var tier = StockTier;
            if (tier <= 0)
                return;

            var rank = GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GuildRank.F;
            foreach (var item in _items)
            {
                if (item && item.IsSoldInArmory && item.ArmoryLevel <= tier && item.UnlockRank <= rank)
                    _onSale.Add(item);
            }
        }

        // NOTE: A demolished armory takes its stock with it.
        private void OnFacilityRemoved(PlacedFacility _)
        {
            if (StockTier > 0)
                return;

            _stock.Clear();
            GameEventsRelay.OnArmoryStockChanged.Invoke();
        }

        // NOTE: The replaced item goes back for a share of its price — paid to the adventurer now, booked with the sales.
        private long SellBack(Adventurer adventurer, EquipmentSlot slot)
        {
            var old = adventurer.Equipment[slot];
            if (!old)
                return 0;

            var refund = (int)(old.Price * _settings.SellBackFraction);
            adventurer.AddWealth(refund);
            adventurer.Equipment.Set(slot, null);
            return refund;
        }

        // NOTE: One wallet change per morning (one HUD float). A negative net (rare) is paid if the guild can afford it.
        private static void Book(long net, int sold)
        {
            if (GuildManager.HasInstance)
            {
                switch (net)
                {
                    case > 0:
                        GuildManager.Instance.Wallet.Add(net, GoldReason.EquipmentSales);
                        break;
                    case < 0:
                        GuildManager.Instance.Wallet.TrySpend(-net, GoldReason.EquipmentSales);
                        break;
                }
            }
            GameEventsRelay.OnArmorySalesCompleted.Invoke(sold, net);
        }
    }
}