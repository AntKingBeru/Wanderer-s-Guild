// Promotion rules (pure): whether an adventurer can take the next rank's exam, its fee, and the exam's pass chance.
namespace WanderersGuild
{
    public static class PromotionRules
    {
        // NOTE: Check order = what the player most needs to know first: busy, progress, level, guild rank, gold.
        public static PromotionCheck Check(Adventurer adventurer, GuildRank guildRank, long guildGold, PromotionSettings settings)
        {
            if (adventurer.Rank >= GuildRank.National)
                return PromotionCheck.HighestRank();

            var target = adventurer.Rank + 1;
            var fee = settings.Fee(target);

            if (adventurer.Status == AdventurerStatus.InExam)
                return PromotionCheck.Blocked(target, fee, "Taking an exam");
            if (adventurer.Status != AdventurerStatus.Idle)
                return PromotionCheck.Blocked(target, fee, "Busy with a quest");
            if (adventurer.RankProgress < settings.ProgressRequired)
                return PromotionCheck.Blocked(target, fee, "Needs more rank progress");

            var minLevel = settings.MinLevel(target);
            if (adventurer.Level < minLevel)
                return PromotionCheck.Blocked(target, fee, $"Needs level {minLevel}");
            if (target > guildRank)
                return PromotionCheck.Blocked(target, fee, $"Guild must be Rank {target}");
            return guildGold < fee ? PromotionCheck.Blocked(target, fee, $"Exam fee {fee:N0}g: not enough gold") : PromotionCheck.Eligible(target, fee);
        }

        // Pass chance: all four stats weighted evenly vs one member of the target rank (the quest formula).
        public static float ExamChance(Adventurer adventurer, GuildRank target, QuestSettings quests)
        {
            var power = adventurer.Stats.Total * 0.25f;
            return QuestCalculator.SuccessChance(power, quests.DifficultyPerMember(target), quests);
        }
    }
}