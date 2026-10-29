// The imported pool of every SpeciesData (built by Database › Load); finds species by id.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Species Database", fileName = "SpeciesDatabase")]
    public class SpeciesDatabase : ScriptableObject
    {
        [SerializeField] private SpeciesData[] species = Array.Empty<SpeciesData>();

        public IReadOnlyList<SpeciesData> Species => species ?? Array.Empty<SpeciesData>();

        // Usable species by id. NOTE: Null entries and empty ids are skipped; for duplicate ids the first wins.
        public Dictionary<string, SpeciesData> BuildLookup() => DatabaseLookup.Build(Species, nameof(SpeciesDatabase));

#if UNITY_EDITOR
        // Editor-only: the importer replaces the list after each load.
        public void SetSpecies(SpeciesData[] items) => species = items ?? Array.Empty<SpeciesData>();
#endif
    }
}