// Facility effect (the Alchemist): potion stock capacity and the highest brewable tier at each finished level.
using System;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Effects/Alchemist", fileName = "AlchemistEffect")]
    public class AlchemistEffect : FacilityEffect
    {
        [Tooltip("Potions the stock holds at each level (element 0 = level 1). Levels past the list use the last value.")]
        [SerializeField] private int[] stockPerLevel = { 4, 6, 8 };

        [Tooltip("Highest brewable tier at each level (element 0 = level 1). Levels past the list use the last value.")]
        [SerializeField] private int[] maxTierPerLevel = { 1, 2, 3 };

        // Stock capacity at a finished level (0 for level 0 — still being built).
        public int StockAt(int level) => ByLevel(stockPerLevel, level);

        // Highest tier at a finished level (0 for level 0).
        public int MaxTierAt(int level) => ByLevel(maxTierPerLevel, level);

        public override string Describe(int level)
        {
            var lv = Math.Max(1, level);
            var tier = MaxTierAt(lv);
            var tiers = GameConfig.HasInstance ? GameConfig.Instance.Alchemy.Tiers : null;
            var potionName = tiers != null && tier >= 1 && tier <= tiers.Count ? tiers[tier - 1].Name : $"tier {tier}";
            return $"{StockAt(lv)} potions · up to {potionName}";
        }

        private static int ByLevel(int[] values, int level)
        {
            if (level < 1 || values == null || values.Length == 0) return 0;
            return Mathf.Max(0, values[Mathf.Min(level, values.Length) - 1]);
        }
    }
}