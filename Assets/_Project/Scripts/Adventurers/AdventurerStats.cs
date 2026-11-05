// The stat formula: class base + species base + (species growth + class bonus) × (level − 1) + retained growth, each ≥ 1.
namespace WanderersGuild
{
    public static class AdventurerStats
    {
        public const int MinimumStat = 1;

        // NOTE: Level is the level in the CURRENT class; growth from earlier classes is carried in retainedGrowth.
        public static StatBlock Compute(ClassData classData, SpeciesData species, int level, StatBlock retainedGrowth)
        {
            var perLevel = species.GrowthPerLevel + classData.StatGrowthPerLevel;
            return (classData.BaseStats + species.BaseStatModifiers + perLevel * (level - 1) + retainedGrowth).AtLeast(MinimumStat);
        }
    }
}