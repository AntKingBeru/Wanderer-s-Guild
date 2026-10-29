// The stat formula: class base + species modifier + class growth × (level − 1) + retained growth, each stat at least 1.
namespace WanderersGuild
{
    public static class AdventurerStats
    {
        public const int MinimumStat = 1;

        public static StatBlock Compute(ClassData classData, SpeciesData species, int level, StatBlock retainedGrowth) =>
            (classData.BaseStats + species.BaseStatModifiers + classData.StatGrowthPerLevel * (level - 1) + retainedGrowth)
            .AtLeast(MinimumStat);
    }
}