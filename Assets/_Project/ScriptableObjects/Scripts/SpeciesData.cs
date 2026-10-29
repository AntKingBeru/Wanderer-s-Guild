// One adventurer species (imported from the Species sheet): name, description and stat modifiers.
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
        [SerializeField] private StatBlock baseStatModifiers;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public StatBlock BaseStatModifiers => baseStatModifiers;

#if UNITY_EDITOR
        // Editor-only: the sheet importer fills or updates this asset in place. NOTE: The icon is left untouched.
        public void ApplyImport(in SpeciesImportRow row)
        {
            id = row.Id;
            displayName = row.DisplayName;
            description = row.Description;
            baseStatModifiers = row.StatModifiers;
        }
#endif
    }
}