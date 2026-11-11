// Plans the dawn restock (pure): every missing unit of every item on sale, cheapest first, as many as the gold allows.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class ArmoryRestockPlan
    {
        private ArmoryRestockPlan(Dictionary<EquipmentData, int> units, long cost, int bought, int missing)
        {
            UnitsByItem = units;
            Cost = cost;
            Bought = bought;
            Missing = missing;
        }

        public IReadOnlyDictionary<EquipmentData, int> UnitsByItem { get; }
        public long Cost { get; }
        public int Bought { get; }

        // Units that were missing before the restocking.
        public int Missing { get; }

        // Wholesale cost of one unit: ⌈price × fraction⌉ (a 1g item never restocks for free).
        public static int UnitCost(EquipmentData item, float fraction) => Mathf.CeilToInt(item.Price * fraction);

        // The whole restocking's cost if gold were unlimited (for the stock screen).
        public static long FullCost(IReadOnlyList<EquipmentData> onSale, ArmoryStock stock, float fraction)
        {
            return onSale.Sum(item => (long)stock.Missing(item) * UnitCost(item, fraction));
        }

        // NOTE: Units are bought one at a time in order of unit cost (then id, for a stable order); the first unit that
        // doesn't fit ends the plan — everything after it costs at least as much.
        public static ArmoryRestockPlan Build(IReadOnlyList<EquipmentData> onSale, ArmoryStock stock, long gold, float fraction)
        {
            List<(EquipmentData item, int cost)> units = new();
            foreach (var item in onSale)
            {
                var missing = stock.Missing(item);
                for (var i = 0; i < missing; i++)
                    units.Add((item, UnitCost(item, fraction)));
            }
            units.Sort((a, b) => a.cost != b.cost ? a.cost.CompareTo(b.cost) : string.CompareOrdinal(a.item.Id, b.item.Id));

            Dictionary<EquipmentData, int> plan = new();
            long spent = 0;
            var bought = 0;
            foreach (var (item, cost) in units)
            {
                if (spent + cost > gold)
                    break;

                spent += cost;
                bought++;
                plan[item] = plan.TryGetValue(item, out var n) ? n + 1 : 1;
            }

            return new ArmoryRestockPlan(plan, spent, bought, units.Count);
        }
    }
}