// Dismissal rules (pure): whether an adventurer can be dismissed now, and the reputation it costs (by rank and level).
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Needs a finished office. Only someone at the guild and free — Idle or Injured; anyone on a quest, signed up,
    // in an exam, a trial or returning can't be (their quest/exam would lose them mid-way).
    public static class DismissalRules
    {
        public static DismissalCheck Check(Adventurer adventurer, bool hasOffice, RecruitmentSettings settings)
        {
            if (adventurer == null)
                return DismissalCheck.No("Nobody selected");

            var loss = ReputationLoss(adventurer, settings);
            if (!hasOffice)
                return DismissalCheck.No("Needs a Guild Master's Office", loss);
            return adventurer.Status is not (AdventurerStatus.Idle or AdventurerStatus.Injured) ? DismissalCheck.No("Busy", loss) : DismissalCheck.Ok(loss);
        }

        // NOTE: Base + per rank (F = 0) + per level above 1, rounded; never negative. Class doesn't matter.
        public static int ReputationLoss(Adventurer adventurer, RecruitmentSettings settings) =>
            Mathf.Max(0, Mathf.RoundToInt(settings.BaseReputationLoss +
                                          settings.ReputationLossPerRank * (int)adventurer.Rank +
                                          settings.ReputationLossPerLevel * (adventurer.Level - 1)));
    }
}