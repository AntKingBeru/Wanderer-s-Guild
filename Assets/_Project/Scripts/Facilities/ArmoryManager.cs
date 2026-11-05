// Game-scene armory: each morning idle adventurers buy their best affordable upgrades; the guild books the net sales once.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Holds no state of its own: gear lives on the adventurers.
    public class ArmoryManager : Singleton<ArmoryManager>
    {
        private static readonly EquipmentSlot[] ShoppingOrder = { EquipmentSlot.Weapon, EquipmentSlot.Offhand, EquipmentSlot.Armor };

        private readonly List<EquipmentData> _stock = new();
        private EquipmentSettings _settings;
        private IReadOnlyList<EquipmentData> _items = System.Array.Empty<EquipmentData>();

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

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Equipment;
            if (_settings.Database)
                _items = _settings.Database.Items;
        }

        private void OnEnable() => GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
        private void OnDisable() => GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);

        // Debug and testing: the morning shopping round, now.
        public void RunShoppingNow() => RunShopping();

        private void OnHourStarted(GameDateTime now)
        {
            if (now.Minute == 0 && now.Hour == _settings.ShoppingHour)
                RunShopping();
        }

        // NOTE: Only idle adventurers shop; the budget is re-read before each slot (wealth changes as they buy).
        private void RunShopping()
        {
            if (!AdventurerManager.HasInstance || !BuildStock())
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
                    var item = EquipmentRules.BestUpgrade(adventurer, slot, _stock, budget, quests);
                    if (!item || !adventurer.TrySpendWealth(item.Price))
                        continue;

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

            if (sold > 0)
                Book(sales - buyBacks, sold);
        }

        // NOTE: On sale = sold in the armory at all (not starter-only), armory tier reached, and the guild's rank reached.
        private bool BuildStock()
        {
            var tier = StockTier;
            _stock.Clear();
            if (tier <= 0)
                return false;

            var rank = GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GuildRank.F;
            foreach (var item in _items)
            {
                if (item && item.IsSoldInArmory && item.ArmoryLevel <= tier && item.UnlockRank <= rank)
                    _stock.Add(item);
            }
            return _stock.Count > 0;
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