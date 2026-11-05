// Facility effect (the Armory): which equipment tier it stocks at each finished level.
using System;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Effects/Armory", fileName = "ArmoryEffect")]
    public class ArmoryEffect : FacilityEffect
    {
        [Tooltip("Stock tier at each level (element 0 = level 1): items with ARMORY_LEVEL up to this are sold. Levels past the " +
                 "list use the last value.")]
        [SerializeField] private int[] stockTierPerLevel = { 1, 2, 3 };

        // Stock tier at a finished level (0 for level 0 — still being built).
        public int TierAt(int level)
        {
            if (level < 1 || stockTierPerLevel == null || stockTierPerLevel.Length == 0)
                return 0;
            return Mathf.Max(0, stockTierPerLevel[Mathf.Min(level, stockTierPerLevel.Length) - 1]);
        }

        public override string Describe(int level) => $"Sells tier {TierAt(Math.Max(1, level))} gear";
    }
}