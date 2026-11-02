// Facility effect (the Guild Master's Office): arrivals become applicants; how many can wait at once per finished level.
using System;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Effects/Applicants", fileName = "ApplicantsEffect")]
    public class ApplicantsEffect : FacilityEffect
    {
        [Tooltip("Applicants that can wait at once at each level (element 0 = level 1). Levels past the list use the last value.")]
        [SerializeField] private int[] maxWaitingPerLevel = { 3, 5 };

        // Waiting slots at a finished level (0 for level 0 — still being built).
        public int MaxWaitingAt(int level)
        {
            if (level < 1 || maxWaitingPerLevel == null || maxWaitingPerLevel.Length == 0) return 0;
            return Mathf.Max(0, maxWaitingPerLevel[Mathf.Min(level, maxWaitingPerLevel.Length) - 1]);
        }

        public override string Describe(int level) => $"Applicants: up to {MaxWaitingAt(Math.Max(1, level))} waiting";
    }
}