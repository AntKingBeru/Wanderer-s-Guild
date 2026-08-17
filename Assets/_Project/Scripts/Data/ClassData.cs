// Authored SO defining an adventurer class and its progression/advancement path.
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(fileName = "ClassData", menuName = "Wanderer's Guild/Data/Class", order = 1)]
    public class ClassData : ScriptableObject, IIdentifiable
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Progression")]
        [SerializeField] private StatBlock baseStats;
        [SerializeField] private StatBlock statGrowthPerLevel;
        [SerializeField] private Rank requiredGuildRank = Rank.F;

        [Header("Advancement")]
        // Referenced by id, not direct SO link, so the import tool can author assets in any order.
        [SerializeField] private string advancesToId;

        [Header("Affinities")]
        [SerializeField] private QuestCategory[] categoryAffinities;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public StatBlock BaseStats => baseStats;
        public StatBlock StatGrowthPerLevel => statGrowthPerLevel;
        public Rank RequiredGuildRank => requiredGuildRank;
        public string AdvancesToId => advancesToId;
        public QuestCategory[] CategoryAffinities => categoryAffinities;

        // Runtime-only factory for placeholder data before real assets exist.
        public static ClassData CreatePlaceholder(string id, string displayName, StatBlock baseStats,
            StatBlock growth, params QuestCategory[] affinities)
        {
            var so = CreateInstance<ClassData>();
            so.id = id;
            so.displayName = displayName;
            so.baseStats = baseStats;
            so.statGrowthPerLevel = growth;
            so.categoryAffinities = affinities;
            return so;
        }
    }
}