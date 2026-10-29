// One adventurer class (imported from the Classes sheet): base stats, growth per level, unlock rank, upgrades, affinities.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Class Data", fileName = "ClassData")]
    public class ClassData : ScriptableObject, IIdentifiable
    {
        [Header("Identity")]
        [Tooltip("Stable, unique id (also the asset's file name) — saves refer to classes by it.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [Tooltip("Not set by the importer (yet) — assign by hand; reloads keep it.")]
        [SerializeField] private Sprite icon;

        [Header("Stats")]
        [SerializeField] private StatBlock baseStats;
        [SerializeField] private StatBlock statGrowthPerLevel;

        [Header("Progression")]
        [Tooltip("Guild rank needed before adventurers of this class can arrive / advance into it.")]
        [SerializeField] private GuildRank requiredGuildRank;
        [Tooltip("Ids of the classes this one can advance to. A class nobody lists here is a base class.")]
        [SerializeField] private string[] advancesToIds = Array.Empty<string>();

        [Header("Quests")]
        [SerializeField] private QuestCategory[] categoryAffinities = Array.Empty<QuestCategory>();

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public StatBlock BaseStats => baseStats;
        public StatBlock StatGrowthPerLevel => statGrowthPerLevel;
        public GuildRank RequiredGuildRank => requiredGuildRank;
        public IReadOnlyList<string> AdvancesToIds => advancesToIds ?? Array.Empty<string>();
        public IReadOnlyList<QuestCategory> CategoryAffinities => categoryAffinities ?? Array.Empty<QuestCategory>();

        public bool HasAffinity(QuestCategory category) => Array.IndexOf(categoryAffinities ?? Array.Empty<QuestCategory>(), category) >= 0;

#if UNITY_EDITOR
        // Editor-only: the sheet importer fills or updates this asset in place. NOTE: The icon is left untouched.
        public void ApplyImport(in ClassImportRow row)
        {
            id = row.Id;
            displayName = row.DisplayName;
            description = row.Description;
            baseStats = row.BaseStats;
            statGrowthPerLevel = row.StatGrowth;
            requiredGuildRank = row.RequiredGuildRank;
            advancesToIds = row.AdvancesToIds ?? Array.Empty<string>();
            categoryAffinities = row.Affinities ?? Array.Empty<QuestCategory>();
        }
#endif
    }
}