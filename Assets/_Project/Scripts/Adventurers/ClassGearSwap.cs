// Re-equips an adventurer for a new class: unusable items come off (their sell-back value is returned), then the new class's
// starting gear fills empty slots (two-handed rules respected).
using System.Collections.Generic;

namespace WanderersGuild
{
    public static class ClassGearSwap
    {
        private static readonly EquipmentSlot[] Slots = { EquipmentSlot.Weapon, EquipmentSlot.Offhand, EquipmentSlot.Armor };

        /// <returns>Total sell-back value of the removed items (the caller pays it).</returns>
        public static int Apply(Adventurer adventurer, ClassData newClass, IReadOnlyDictionary<string, EquipmentData> items,
                                float sellBackFraction)
        {
            var refund = 0;
            foreach (var slot in Slots)
            {
                var item = adventurer.Equipment[slot];
                if (item == null || item.CanBeUsedBy(newClass))
                    continue;

                refund += (int)(item.Price * sellBackFraction);
                adventurer.Equipment.Set(slot, null);
            }

            foreach (var id in newClass.StartingGearIds)
            {
                if (!items.TryGetValue(id, out var item) || !item.CanBeUsedBy(newClass))
                    continue;
                if (adventurer.Equipment[item.Slot] != null)
                    continue;
                if (!Fits(adventurer, item))
                    continue;

                adventurer.Equipment.Set(item.Slot, item);
            }
            return refund;
        }

        // NOTE: A two-handed weapon needs an empty offhand; an offhand can't go next to a two-handed weapon.
        private static bool Fits(Adventurer adventurer, EquipmentData item)
        {
            switch (item.Slot)
            {
                case EquipmentSlot.Weapon when item.TwoHanded:
                    return adventurer.Equipment[EquipmentSlot.Offhand] == null;
                case EquipmentSlot.Offhand:
                {
                    var weapon = adventurer.Equipment[EquipmentSlot.Weapon];
                    return weapon == null || !weapon.TwoHanded;
                }
                default:
                    return true;
            }
        }
    }
}