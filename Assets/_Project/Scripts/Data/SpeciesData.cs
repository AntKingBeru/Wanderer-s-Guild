// Authored SO defining an adventurer species. Instances are written by the DB import tool.
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(fileName = "SpeciesData", menuName = "Wanderer's Guild/Data/Species", order = 0)]
    public class SpeciesData : ScriptableObject, IIdentifiable
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Traits")]
        [SerializeField] private StatBlock baseStatModifiers;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public StatBlock BaseStatModifiers => baseStatModifiers;

        // Runtime-only factory for placeholder data before real assets exist.
        public static SpeciesData CreatePlaceholder(string id, string displayName, StatBlock mods)
        {
            var so = CreateInstance<SpeciesData>();
            so.id = id;
            so.displayName = displayName;
            so.baseStatModifiers = mods;
            return so;
        }
    }
}