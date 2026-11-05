// The armory's units per item (pure state): take one, top up to each item's STOCK, clear, save/restore.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class ArmoryStock
    {
        private readonly Dictionary<EquipmentData, int> _units = new();

        public int Units(EquipmentData item) => item && _units.TryGetValue(item, out var units) ? units : 0;

        // Units needed to reach the item's STOCK (0 if full).
        public int Missing(EquipmentData item) => Mathf.Max(0, item.MaxStock - Units(item));

        // Takes one unit. False (nothing taken) if sold out.
        public bool TryTake(EquipmentData item)
        {
            var units = Units(item);
            if (units <= 0)
                return false;

            _units[item] = units - 1;
            return true;
        }

        // Adds units, never past the item's STOCK.
        public void Add(EquipmentData item, int units)
        {
            if (!item || units <= 0)
                return;
            _units[item] = Mathf.Min(item.MaxStock, Units(item) + units);
        }

        public void Clear() => _units.Clear();

        public List<SavedStockItem> ToSaved()
        {
            return (from pair in _units where pair.Value > 0 select new SavedStockItem(pair.Key.Id, pair.Value)).ToList();
        }

        // NOTE: Unknown ids (removed from the sheet) are dropped with a warning; units are clamped to the item's STOCK.
        public void Restore(IReadOnlyList<SavedStockItem> saved, IReadOnlyDictionary<string, EquipmentData> lookup)
        {
            _units.Clear();
            foreach (var entry in saved)
            {
                if (string.IsNullOrEmpty(entry.ItemId) || !lookup.TryGetValue(entry.ItemId, out EquipmentData item))
                {
                    Debug.LogWarning($"[ArmoryStock] Saved stock of unknown item '{entry.ItemId}'; dropped.");
                    continue;
                }
                Add(item, entry.Units);
            }
        }
    }
}