// Promotion rules (pure): whether an adventurer can take the next rank's exam, its fee, and the solo pass chances used by
// exams (eased by the exam scale) and class trials (stats + Well fed vs a rank's per-member difficulty).
namespace WanderersGuild
{
    public static class PromotionRules
    {
        // NOTE: Check order = what the player most needs to know first: busy, class cap, progress, level, guild rank, gold.
        public static PromotionCheck Check(Adventurer adventurer, GuildRank guildRank, long guildGold, PromotionSettings settings)
        {
            if (adventurer.Rank >= GuildRank.National)
                return PromotionCheck.HighestRank();

            var target = adventurer.Rank + 1;
            var fee = settings.Fee(target);

            if (adventurer.Status == AdventurerStatus.InExam)
                return PromotionCheck.Blocked(target, fee, "Taking an exam");
            if (adventurer.Status == AdventurerStatus.InTrial)
                return PromotionCheck.Blocked(target, fee, "Taking a class trial");
            if (adventurer.Status != AdventurerStatus.Idle)
                return PromotionCheck.Blocked(target, fee, "Busy with a quest");

            var cap = adventurer.Class.MaxRank;
            if (target > cap)
                return PromotionCheck.Blocked(target, fee, $"{adventurer.Class.DisplayName} can't rank above {cap}: advance class first");

            if (adventurer.RankProgress < settings.ProgressRequired)
                return PromotionCheck.Blocked(target, fee, "Needs more rank progress");

            var minLevel = settings.MinLevel(target);
            if (adventurer.Level < minLevel)
                return PromotionCheck.Blocked(target, fee, $"Needs level {minLevel}");
            if (target > guildRank)
                return PromotionCheck.Blocked(target, fee, $"Guild must be Rank {target}");
            return guildGold < fee ? PromotionCheck.Blocked(target, fee, $"Exam fee {fee:N0}g: not enough gold") : PromotionCheck.Eligible(target, fee);
        }

        // Solo pass chance for a class trial: the full difficulty of one member of the given rank.
        // NOTE: Rolled when they return; supper never touches someone away, so the buff they left with is the one that counts.
        public static float ExamChance(Adventurer adventurer, GuildRank against, QuestSettings quests) =>
            SoloChance(adventurer, quests.DifficultyPerMember(against), quests);

        // Solo pass chance for a promotion exam: the target rank's per-member difficulty × the exam scale (easier than a quest).
        public static float PromotionExamChance(Adventurer adventurer, GuildRank target, QuestSettings quests, PromotionSettings settings) =>
            SoloChance(adventurer, quests.DifficultyPerMember(target) * settings.ExamDifficultyScale, quests);

        // NOTE: All four stats weighted evenly, × (1 + Well fed) — the quest formula for one member.
        private static float SoloChance(Adventurer adventurer, float difficulty, QuestSettings quests)
        {
            var power = adventurer.Stats.Total * 0.25f * (1f + adventurer.WellFedBonus);
            return QuestCalculator.SuccessChance(power, difficulty, quests);
        }
    }
}