// Shopping rules (pure): how much an item is worth to a class, and the best affordable upgrade for one slot.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class EquipmentRules
    {
        // The item's stats weighted like the class's quests: the average of its affinity categories' weights,
        // or even weights (¼ each) if it has none.
        public static float Value(EquipmentData item, ClassData classData, QuestSettings quests)
        {
            if (!item)
                return 0f;

            var affinities = classData.CategoryAffinities;
            if (affinities.Count == 0)
                return item.Stats.Total * 0.25f;

            var total = affinities.Sum(category => quests.Weights(category).Apply(item.Stats));
            return total / affinities.Count;
        }

        // The best item for this slot that's on sale, usable, affordable and strictly better than what's worn
        // (a two-handed weapon is compared with weapon + offhand); null if none. Ties go to the cheaper item.
        public static EquipmentData BestUpgrade(Adventurer adventurer, EquipmentSlot slot, IReadOnlyList<EquipmentData> stock,
                                                int budget, QuestSettings quests)
        {
            var gear = adventurer.Equipment;
            if (slot == EquipmentSlot.Offhand && gear[EquipmentSlot.Weapon] && gear[EquipmentSlot.Weapon].TwoHanded)
                return null;

            EquipmentData best = null;
            var bestGain = 0f;
            foreach (var item in stock)
            {
                if (item.Slot != slot || item.Price > budget || !item.CanBeUsedBy(adventurer.Class))
                    continue;

                var current = Value(gear[slot], adventurer.Class, quests);
                if (item.TwoHanded) current += Value(gear[EquipmentSlot.Offhand], adventurer.Class, quests);

                var gain = Value(item, adventurer.Class, quests) - current;
                if (gain <= 0.0001f)
                    continue;
                if (best && (gain < bestGain || (Mathf.Approximately(gain, bestGain) && item.Price >= best.Price)))
                    continue;

                best = item;
                bestGain = gain;
            }
            return best;
        }
    }
}