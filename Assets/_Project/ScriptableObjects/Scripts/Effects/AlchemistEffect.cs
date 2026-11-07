// Facility effect (the Alchemist): the highest potion tier it can brew at each finished level.
using System;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Effects/Alchemist", fileName = "AlchemistEffect")]
    public class AlchemistEffect : FacilityEffect
    {
        [Tooltip("Highest brewable tier at each level (element 0 = level 1). Levels past the list use the last value.")]
        [SerializeField] private int[] maxTierPerLevel = { 1, 2, 3 };

        // Highest tier at a finished level (0 for level 0 — still being built).
        public int MaxTierAt(int level)
        {
            if (level < 1 || maxTierPerLevel == null || maxTierPerLevel.Length == 0)
                return 0;
            return Mathf.Max(0, maxTierPerLevel[Mathf.Min(level, maxTierPerLevel.Length) - 1]);
        }

        public override string Describe(int level)
        {
            var tier = MaxTierAt(Math.Max(1, level));
            var tiers = GameConfig.HasInstance ? GameConfig.Instance.Alchemy.Tiers : null;
            var potionName = tiers != null && tier >= 1 && tier <= tiers.Count ? tiers[tier - 1].Name : $"tier {tier}";
            return $"Brews potions up to {potionName}";
        }
    }
}