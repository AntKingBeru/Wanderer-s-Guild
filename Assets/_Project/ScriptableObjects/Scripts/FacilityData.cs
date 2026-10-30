// One facility type (imported from the Facilities sheet): size, levels with cost/days/unlock rank, limit; prefabs set in Unity.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Data", fileName = "FacilityData")]
    public class FacilityData : ScriptableObject, IIdentifiable
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [Tooltip("Not set by the importer — assign by hand; reloads keep it.")]
        [SerializeField] private Sprite icon;

        [Header("Footprint")]
        [Tooltip("Width (x) × depth (z) in cells, unrotated.")]
        [SerializeField] private Vector2Int size = new(4, 4);

        [Header("Levels")]
        [SerializeField, Min(1)] private int maxLevel = 1;
        [SerializeField] private long[] costs = Array.Empty<long>();
        [SerializeField] private int[] buildDays = Array.Empty<int>();
        [SerializeField] private GuildRank[] unlockRanks = Array.Empty<GuildRank>();
        [Tooltip("Most copies the guild may build (0 = unlimited).")]
        [SerializeField, Min(0)] private int limit;

        [Header("Models (not set by the importer)")]
        [Tooltip("One prefab per level (element 0 = level 1). Missing levels use the previous level's prefab. Pivot at the " +
                 "footprint's min corner; footprint along +X (width) and +Z (depth); FacilityConnector markers on the level-1 prefab.")]
        [SerializeField] private GameObject[] levelPrefabs = Array.Empty<GameObject>();

        private ConnectorSpec[] _connectors;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Vector2Int Size => new(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public int Limit => limit;

        // Cost to reach this level (1-based). 0 if the sheet has no value.
        public long CostFor(int level) => ByLevel(costs, level, 0L);
        public int BuildDaysFor(int level) => Mathf.Max(0, ByLevel(buildDays, level, 1));
        public GuildRank UnlockRankFor(int level) => ByLevel(unlockRanks, level, GuildRank.F);

        // The prefab for a level (falls back to the nearest lower level), or null if none are assigned.
        public GameObject PrefabFor(int level)
        {
            for (var i = Mathf.Clamp(level, 1, MaxLevel) - 1; i >= 0; i--)
            {
                if (levelPrefabs != null && i < levelPrefabs.Length && levelPrefabs[i])
                    return levelPrefabs[i];
            }
            return null;
        }

        // Connectors from the level-1 prefab's FacilityConnector markers (read once, cached).
        public IReadOnlyList<ConnectorSpec> Connectors
        {
            get
            {
                if (_connectors != null)
                    return _connectors;

                var prefab = PrefabFor(1);
                FacilityConnector[] markers = prefab ? prefab.GetComponentsInChildren<FacilityConnector>(true) : Array.Empty<FacilityConnector>();
                _connectors = new ConnectorSpec[markers.Length];
                for (var i = 0; i < markers.Length; i++)
                    _connectors[i] = new ConnectorSpec(markers[i].Cell, markers[i].Facing);
                return _connectors;
            }
        }

        private static T ByLevel<T>(T[] values, int level, T fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];

#if UNITY_EDITOR
        // Editor-only: the sheet importer fills or updates this asset in place. NOTE: Prefabs and icon are untouched.
        public void ApplyImport(in FacilityImportRow row)
        {
            id = row.Id;
            displayName = row.DisplayName;
            description = row.Description;
            size = row.Size;
            maxLevel = row.MaxLevel;
            costs = row.Costs ?? Array.Empty<long>();
            buildDays = row.BuildDays ?? Array.Empty<int>();
            unlockRanks = row.UnlockRanks ?? Array.Empty<GuildRank>();
            limit = row.Limit;
            _connectors = null;
        }

        private void OnValidate() => _connectors = null;
#endif
    }
}