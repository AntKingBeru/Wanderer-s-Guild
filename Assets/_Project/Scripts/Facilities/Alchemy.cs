// Potion rules (pure, reads the rooms and the tier settings): brewable tier, tier data, ingredient cost, who'd refuse, preview gold.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class Alchemy
    {
        // Highest brewable tier from every finished room's AlchemistEffect, capped by the tiers defined (0 = none).
        public static int MaxTier
        {
            get
            {
                if (!FacilityManager.HasInstance)
                    return 0;

                var tier = 0;
                foreach (var room in FacilityManager.Instance.Rooms)
                {
                    AlchemistEffect effect = room.Data.Effect<AlchemistEffect>();
                    if (effect)
                        tier = Mathf.Max(tier, effect.MaxTierAt(room.Level));
                }
                return Mathf.Min(tier, GameConfig.Instance.Alchemy.Tiers.Count);
            }
        }

        // Tier data (1-based). False for 0 or a tier that isn't defined.
        public static bool TryGetTier(int tier, out PotionTier data)
        {
            IReadOnlyList<PotionTier> tiers = GameConfig.Instance.Alchemy.Tiers;
            var valid = tier >= 1 && tier <= tiers.Count;
            data = valid ? tiers[tier - 1] : default;
            return valid;
        }

        public static long IngredientCost(PotionTier tier, int members) => (long)tier.IngredientCostPerMember * Mathf.Max(0, members);

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
    }
}