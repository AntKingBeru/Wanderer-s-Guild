// Class advancement rules (pure): who's at their class cap, which upgrades they may take, the fee, the trial's pass chance,
// and the retained growth that keeps every stat from dropping.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class AdvancementRules
    {
        // At the level cap AND at the class's rank cap — advancing is the only way forward.
        public static bool IsAtClassCap(Adventurer adventurer) =>
            adventurer.IsMaxLevel && adventurer.Rank >= adventurer.Class.MaxRank;

        // The trial fee: the next rank's promotion fee × the multiplier.
        public static long Fee(Adventurer adventurer, PromotionSettings promotion, AdvancementSettings settings) =>
            (long)Mathf.Round(promotion.Fee(NextRank(adventurer.Rank)) * settings.FeeMultiplier);

        // NOTE: Order = what the player most needs to know first: busy, upgrades exist, guild rank, gold.
        public static AdvancementCheck Check(Adventurer adventurer, GuildRank guildRank, long guildGold,
                                             PromotionSettings promotion, AdvancementSettings settings,
                                             IReadOnlyDictionary<string, ClassData> classes)
        {
            if (!IsAtClassCap(adventurer))
                return AdvancementCheck.NotAtCap();

            var fee = Fee(adventurer, promotion, settings);
            if (adventurer.Status == AdventurerStatus.InTrial)
                return AdvancementCheck.Blocked(fee, "Taking a class trial");
            if (adventurer.Status != AdventurerStatus.Idle)
                return AdvancementCheck.Blocked(fee, "Busy");
            if (adventurer.Class.AdvancesToIds.Count == 0)
                return AdvancementCheck.Blocked(fee, "No further class");

            List<ClassData> options = new();
            var lowestNeeded = GuildRank.National;
            foreach (var id in adventurer.Class.AdvancesToIds)
            {
                if (!classes.TryGetValue(id, out var option))
                    continue;
                if (option.RequiredGuildRank <= guildRank)
                    options.Add(option);
                else if (option.RequiredGuildRank < lowestNeeded)
                    lowestNeeded = option.RequiredGuildRank;
            }

            if (options.Count == 0)
                return AdvancementCheck.Blocked(fee, $"Guild must be Rank {lowestNeeded}");
            return guildGold < fee ? AdvancementCheck.Blocked(fee, $"Trial fee {fee:N0}g: not enough gold") : AdvancementCheck.Eligible(options, fee);
        }

        // Pass chance: the solo exam formula against their CURRENT rank (advancing doesn't raise the rank — they're
        // proving the rank they hold, at their class's cap).
        public static float TrialChance(Adventurer adventurer, QuestSettings quests) =>
            PromotionRules.ExamChance(adventurer, adventurer.Rank, quests);

        // Retained growth after advancing: everything grown in the old class, topped up so that at level 1 in the new
        // class no stat is lower than before (gear not included).
        public static StatBlock RetainedAfter(Adventurer adventurer, ClassData newClass)
        {
            var perLevel = adventurer.Species.GrowthPerLevel + adventurer.Class.StatGrowthPerLevel;
            var retained = adventurer.RetainedGrowth + perLevel * (adventurer.Level - 1);

            var before = adventurer.BaseStats;
            var after = AdventurerStats.Compute(newClass, adventurer.Species, 1, retained);
            return retained + Shortfall(before, after);
        }

        private static StatBlock Shortfall(StatBlock want, StatBlock have) =>
            new(Mathf.Max(0, want.Strength - have.Strength), Mathf.Max(0, want.Agility - have.Agility),
                Mathf.Max(0, want.Intellect - have.Intellect), Mathf.Max(0, want.Vitality - have.Vitality));

        private static GuildRank NextRank(GuildRank rank) => rank >= GuildRank.National ? GuildRank.National : rank + 1;
    }
}