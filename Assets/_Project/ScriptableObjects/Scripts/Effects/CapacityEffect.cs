// Facility effect: adventurer beds per finished level (e.g. Guild Hall 4; Bedroom +2 / +4 / +6).
using System;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Effects/Capacity", fileName = "CapacityEffect")]
    public class CapacityEffect : FacilityEffect
    {
        [Tooltip("Beds at each level (element 0 = level 1). Levels past the list use the last value.")]
        [SerializeField] private int[] bedsPerLevel = { 2, 4, 6 };

        // Beds at a finished level (0 for level 0 — still being built).
        public int CapacityAt(int level)
        {
            if (level < 1 || bedsPerLevel == null || bedsPerLevel.Length == 0)
                return 0;
            return Mathf.Max(0, bedsPerLevel[Mathf.Min(level, bedsPerLevel.Length) - 1]);
        }

        public override string Describe(int level) => $"Capacity +{CapacityAt(Math.Max(1, level))}";
    }
}