// The imported pool of every FacilityData (built by Database › Load); finds facilities by id.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Facility Database", fileName = "FacilityDatabase")]
    public class FacilityDatabase : ScriptableObject
    {
        [SerializeField] private FacilityData[] facilities = Array.Empty<FacilityData>();

        public IReadOnlyList<FacilityData> Facilities => facilities ?? Array.Empty<FacilityData>();

        // Usable facilities by id. NOTE: Null entries and empty ids are skipped; for duplicate ids the first wins.
        public Dictionary<string, FacilityData> BuildLookup() => DatabaseLookup.Build(Facilities, nameof(FacilityDatabase));

#if UNITY_EDITOR
        // Editor-only: the importer replaces the list after each load.
        public void SetFacilities(FacilityData[] items) => facilities = items ?? Array.Empty<FacilityData>();
#endif
    }
}