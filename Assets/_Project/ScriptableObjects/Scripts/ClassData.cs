// One adventurer class (imported from the Classes sheet): base stats, growth bonus, unlock + max rank, upgrades, affinities, starting gear.
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
        [Tooltip("The class's bonus per level, added on top of the species' growth.")]
        [SerializeField] private StatBlock statGrowthPerLevel;

        [Header("Progression")]
        [Tooltip("Guild rank needed before adventurers of this class can arrive / advance into it.")]
        [SerializeField] private GuildRank requiredGuildRank;
        [Tooltip("Highest adventurer rank this class can reach; beyond it, the adventurer must advance to another class.")]
        [SerializeField] private GuildRank maxRank = GuildRank.National;
        [Tooltip("Ids of the classes this one can advance to. A class nobody lists here is a base class.")]
        [SerializeField] private string[] advancesToIds = Array.Empty<string>();

        [Header("Quests")]
        [SerializeField] private QuestCategory[] categoryAffinities = Array.Empty<QuestCategory>();

        [Header("Gear")]
        [Tooltip("Equipment ids every new adventurer of this class arrives wearing (starter items: ARMORY_LEVEL 0).")]
        [SerializeField] private string[] startingGearIds = Array.Empty<string>();

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public StatBlock BaseStats => baseStats;
        public StatBlock StatGrowthPerLevel => statGrowthPerLevel;
        public GuildRank RequiredGuildRank => requiredGuildRank;
        public GuildRank MaxRank => maxRank;
        public IReadOnlyList<string> AdvancesToIds => advancesToIds ?? Array.Empty<string>();
        public IReadOnlyList<QuestCategory> CategoryAffinities => categoryAffinities ?? Array.Empty<QuestCategory>();
        public IReadOnlyList<string> StartingGearIds => startingGearIds ?? Array.Empty<string>();

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
            maxRank = row.MaxRank;
            advancesToIds = row.AdvancesToIds ?? Array.Empty<string>();
            categoryAffinities = row.Affinities ?? Array.Empty<QuestCategory>();
            startingGearIds = row.StartingGearIds ?? Array.Empty<string>();
        }
#endif
    }
}