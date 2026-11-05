// The imported pool of every EquipmentData (built by Database › Load); finds items by id.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Equipment Database", fileName = "EquipmentDatabase")]
    public class EquipmentDatabase : ScriptableObject
    {
        [SerializeField] private EquipmentData[] items = Array.Empty<EquipmentData>();

        public IReadOnlyList<EquipmentData> Items => items ?? Array.Empty<EquipmentData>();

        // Usable items by id. NOTE: Null entries and empty ids are skipped; for duplicate ids the first wins.
        public Dictionary<string, EquipmentData> BuildLookup() => DatabaseLookup.Build(Items, nameof(EquipmentDatabase));

#if UNITY_EDITOR
        // Editor-only: the importer replaces the list after each load.
        public void SetItems(EquipmentData[] list) => items = list ?? Array.Empty<EquipmentData>();
#endif
    }
}