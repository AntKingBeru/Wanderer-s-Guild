// One adventurer species (imported from the Species sheet): name, description, base stats and growth per level.
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Species Data", fileName = "SpeciesData")]
    public class SpeciesData : ScriptableObject, IIdentifiable
    {
        [Tooltip("Stable, unique id (also the asset's file name) — saves refer to species by it.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [Tooltip("Not set by the importer (yet) — assign by hand; reloads keep it.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Names for this species' adventurers — assign by hand (reloads keep it; Tools › Wanderer's Guild › " +
                 "Adventurers › Create Species Name Lists makes and assigns defaults). Empty = GameConfig › Adventurers › Name List.")]
        [SerializeField] private AdventurerNameList nameList;
        [SerializeField] private StatBlock baseStatModifiers;
        [Tooltip("General growth per level (every class); the class adds its own bonus on top.")]
        [SerializeField] private StatBlock growthPerLevel;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public AdventurerNameList NameList => nameList;
        public StatBlock BaseStatModifiers => baseStatModifiers;
        public StatBlock GrowthPerLevel => growthPerLevel;

#if UNITY_EDITOR
        // >Editor-only: the sheet importer fills or updates this asset in place. NOTE: The icon is left untouched.
        public void ApplyImport(in SpeciesImportRow row)
        {
            id = row.Id;
            displayName = row.DisplayName;
            description = row.Description;
            baseStatModifiers = row.StatModifiers;
            growthPerLevel = row.Growth;
        }
        
        // Editor-only: the name-list tool assigns a list. NOTE: ApplyImport never touches it, so sheet reloads keep it.
        public void SetNameList(AdventurerNameList list) => nameList = list;
#endif
    }
}