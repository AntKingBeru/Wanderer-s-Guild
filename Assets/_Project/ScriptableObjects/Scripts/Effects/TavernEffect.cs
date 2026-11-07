// Facility effect (the Tavern): supper price and the "Well fed" power bonus at each finished level.
using System;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Effects/Tavern", fileName = "TavernEffect")]
    public class TavernEffect : FacilityEffect
    {
        [Tooltip("Supper price per adventurer at each level (element 0 = level 1). Levels past the list use the last value.")]
        [SerializeField] private int[] mealPricePerLevel = { 3, 5, 8 };

        [Tooltip("Quest power bonus while Well fed at each level (0.06 = +6%).")]
        [SerializeField] private float[] powerBonusPerLevel = { 0.06f, 0.10f, 0.15f };

        public int MealPriceAt(int level) => Mathf.Max(0, ByLevel(mealPricePerLevel, level, 0));
        public float PowerBonusAt(int level) => Mathf.Max(0f, ByLevel(powerBonusPerLevel, level, 0f));

        public override string Describe(int level)
        {
            var lv = Math.Max(1, level);
            return $"Supper {MealPriceAt(lv)}g · +{Mathf.RoundToInt(PowerBonusAt(lv) * 100f)}% power";
        }

        // NOTE: Level 0 (still being built) gives nothing.
        private static T ByLevel<T>(T[] values, int level, T fallback) =>
            level < 1 || values == null || values.Length == 0 ? fallback : values[Mathf.Min(level, values.Length) - 1];
    }
}