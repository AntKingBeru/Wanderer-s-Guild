// An adventurer's worn gear: one item per slot, the combined stat bonus, and the saved form (ids by slot).
using System;
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class AdventurerEquipment
    {
        private static readonly int SlotCount = Enum.GetValues(typeof(EquipmentSlot)).Length;
        private readonly EquipmentData[] _slots = new EquipmentData[SlotCount];

        public EquipmentData this[EquipmentSlot slot] => _slots[(int)slot];

        // The sum of every worn item's stats.
        public StatBlock Bonus
        {
            get
            {
                return _slots.Where(item => item).Aggregate<EquipmentData, StatBlock>(default, (current, item) => current + item.Stats);
            }
        }

        public IEnumerable<EquipmentData> Worn
        {
            get { return _slots.Where(item => item); }
        }

        // Puts an item in its own slot (null clears the given slot).
        public void Set(EquipmentSlot slot, EquipmentData item) => _slots[(int)slot] = item;

        // Item ids by slot index ("" = empty) — the saved form.
        public string[] ToIds()
        {
            var ids = new string[SlotCount];
            for (var i = 0; i < SlotCount; i++)
                ids[i] = _slots[i] ? _slots[i].Id : string.Empty;
            return ids;
        }
    }
}