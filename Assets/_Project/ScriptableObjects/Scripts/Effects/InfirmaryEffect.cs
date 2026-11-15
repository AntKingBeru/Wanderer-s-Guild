// Facility effect (the Infirmary): how much faster injured adventurers recover at each finished level.
using System;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Effects/Infirmary", fileName = "InfirmaryEffect")]
    public class InfirmaryEffect : FacilityEffect
    {
        [Tooltip("Recovery speed at each level (element 0 = level 1; 1.5 = half again as fast). Levels past the list use the last value.")]
        [SerializeField] private float[] recoverySpeedPerLevel = { 1.5f, 2f, 3f };

        // NOTE: Level 0 (still being built) gives nothing (×1); never slower than normal.
        public float RecoverySpeedAt(int level)
        {
            if (level < 1 || recoverySpeedPerLevel == null || recoverySpeedPerLevel.Length == 0)
                return 1f;
            return Mathf.Max(1f, recoverySpeedPerLevel[Mathf.Min(level, recoverySpeedPerLevel.Length) - 1]);
        }

        public override string Describe(int level) => $"Recovery ×{RecoverySpeedAt(Math.Max(1, level)):0.#}";
    }
}