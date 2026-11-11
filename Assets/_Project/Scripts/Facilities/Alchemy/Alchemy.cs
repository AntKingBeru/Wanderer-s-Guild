// Potion rules (reads the rooms and the tier settings): capacity, brewable tier, tier data, stocked potions for a category,
// who'd refuse a share cut, preview gold.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class Alchemy
    {
        // Stock capacity from the best finished alchemist (0 = none).
        public static int Capacity => Best((effect, level) => effect.StockAt(level));

        // Highest brewable tier from the best finished alchemist, capped by the tiers defined (0 = none).
        public static int MaxTier => Mathf.Min(Best((effect, level) => effect.MaxTierAt(level)), GameConfig.Instance.Alchemy.Tiers.Count);

        // Tier data (1-based). False for 0 or a tier that isn't defined.
        public static bool TryGetTier(int tier, out PotionTier data)
        {
            var tiers = GameConfig.Instance.Alchemy.Tiers;
            var valid = tier >= 1 && tier <= tiers.Count;
            data = valid ? tiers[tier - 1] : default;
            return valid;
        }

        // Potions in stock for this category, lowest tier first (filled into <paramref name="results"/>).
        public static void StockedFor(QuestCategory category, PotionStock stock, List<PotionKey> results)
        {
            results.Clear();
            var tiers = GameConfig.Instance.Alchemy.Tiers.Count;
            for (var tier = 1; tier <= tiers; tier++)
            {
                PotionKey key = new(tier, category);
                if (stock.Count(key) > 0)
                    results.Add(key);
            }
        }

        // The share after a cut (never below 0).
        public static int ShareAfterCut(Quest quest, int cut) => Mathf.Max(0, quest.Terms.RewardSharePercent - cut);

        // Gold each member of a full party would get with this share cut (rounding like Quest.GoldPerMember).
        public static int GoldPerMemberWith(Quest quest, int cut)
        {
            var party = (int)((long)quest.Request.Reward.Gold * ShareAfterCut(quest, cut) / 100);
            return party / Mathf.Max(1, quest.Terms.PartySize);
        }

        // The first member whose appeal at the reduced share falls below 1 (they'd refuse), or null.
        public static Adventurer FindRefuser(Quest quest, IReadOnlyList<Adventurer> members, PotionTier tier, QuestSettings settings)
        {
            var gold = GoldPerMemberWith(quest, tier.ShareCutPercent);
            return members.FirstOrDefault(member => QuestCalculator.Appeal(member, quest, settings, gold) < 1f);
        }

        // NOTE: The best value of an effect over every finished alchemist (rooms don't stack).
        private static int Best(System.Func<AlchemistEffect, int, int> value)
        {
            if (!FacilityManager.HasInstance)
                return 0;

            var best = 0;
            foreach (var room in FacilityManager.Instance.Rooms)
            {
                var effect = room.Data.Effect<AlchemistEffect>();
                if (effect is not null)
                    best = Mathf.Max(best, value(effect, room.Level));
            }
            return best;
        }
    }
}