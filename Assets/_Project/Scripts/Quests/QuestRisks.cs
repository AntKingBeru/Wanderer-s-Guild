// Quest risk rolls (pure): per member — death (badly outmatched failures only), then injury and its severity and recovery days.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class QuestRisks
    {
        /// <param name="members"></param>
        /// <param name="success"></param>
        /// <param name="powerRatio">Party power ÷ difficulty (1 = an even fight).</param>
        /// <param name="settings"></param>
        /// <returns>One result per member who was hurt or died (unharmed members aren't listed).</returns>
        // NOTE: Failure: death is checked first (only below the death threshold), then injury (more likely and more severe
        // the more outmatched). Success: a small chance of a Light injury.
        public static List<RiskResult> Roll(IReadOnlyList<Adventurer> members, bool success, float powerRatio, InjurySettings settings)
        {
            List<RiskResult> results = new();
            var outmatch = Mathf.Clamp01(1f - powerRatio);

            foreach (var member in members)
            {
                if (success)
                {
                    if (Random.value < settings.SuccessInjuryChance)
                        results.Add(Injured(member, InjurySeverity.Light, settings));
                    continue;
                }

                if (Random.value < DeathChance(powerRatio, settings))
                {
                    results.Add(new RiskResult(member, true, InjurySeverity.None, 0));
                    continue;
                }

                var injuryChance = settings.FailureInjuryChance + settings.FailureInjuryPerOutmatch * outmatch;
                if (Random.value < injuryChance)
                    results.Add(Injured(member, RollSeverity(outmatch, settings), settings));
            }

            return results;
        }

        public static float DeathChance(float powerRatio, InjurySettings settings)
        {
            var deficit = settings.DeathRatioThreshold - powerRatio;
            return deficit <= 0f ? 0f : Mathf.Min(settings.MaxDeathChance, deficit * settings.DeathChancePerDeficit);
        }

        private static RiskResult Injured(Adventurer member, InjurySeverity severity, InjurySettings settings)
        {
            var days = settings.RecoveryDays(severity);
            return new RiskResult(member, false, severity, Random.Range(days.x, days.y + 1));
        }

        // NOTE: Weights blend from the even-fight set to the outmatched set; a weight set summing to 0 falls back to Light.
        private static InjurySeverity RollSeverity(float outmatch, InjurySettings settings)
        {
            var w = Vector3.Lerp(settings.EvenWeights, settings.OutmatchedWeights, outmatch);
            float light = Mathf.Max(0f, w.x), serious = Mathf.Max(0f, w.y), grave = Mathf.Max(0f, w.z);
            var total = light + serious + grave;
            if (total <= 0f)
                return InjurySeverity.Light;

            var roll = Random.value * total;
            if (roll < light)
                return InjurySeverity.Light;
            return roll < light + serious ? InjurySeverity.Serious : InjurySeverity.Grave;
        }
    }
}