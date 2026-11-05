// One item (imported from the Equipment sheet): slot, allowed classes, stat bonus, price, armory level (0 = starter only), unlock rank.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Equipment Data", fileName = "EquipmentData")]
    public class EquipmentData : ScriptableObject, IIdentifiable
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [Tooltip("Not set by the importer — assign by hand; reloads keep it.")]
        [SerializeField] private Sprite icon;

        [SerializeField] private EquipmentSlot slot;
        [Tooltip("Class ids that may use it. Empty = any class.")]
        [SerializeField] private string[] classIds = Array.Empty<string>();
        [SerializeField] private StatBlock stats;
        [SerializeField, Min(0)] private int price;
        [Tooltip("Armory stock tier needed to sell it. 0 = never sold (starting gear only).")]
        [SerializeField, Min(0)] private int armoryLevel = 1;
        [SerializeField] private GuildRank unlockRank = GuildRank.F;
        [Tooltip("Weapons only: holding it leaves no hand for an offhand.")]
        [SerializeField] private bool twoHanded;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public EquipmentSlot Slot => slot;
        public IReadOnlyList<string> ClassIds => classIds ?? Array.Empty<string>();
        public StatBlock Stats => stats;
        public int Price => price;
        public int ArmoryLevel => armoryLevel;
        public GuildRank UnlockRank => unlockRank;
        public bool TwoHanded => twoHanded && slot == EquipmentSlot.Weapon;

        // False for starter-only gear (ARMORY_LEVEL 0).
        public bool IsSoldInArmory => armoryLevel >= 1;

        public bool CanBeUsedBy(ClassData classData) =>
            classIds == null || classIds.Length == 0 || Array.IndexOf(classIds, classData.Id) >= 0;

#if UNITY_EDITOR
        // Editor-only: the sheet importer fills or updates this asset in place. NOTE: The icon is untouched.
        public void ApplyImport(in EquipmentImportRow row)
        {
            id = row.Id;
            displayName = row.DisplayName;
            description = row.Description;
            slot = row.Slot;
            classIds = row.ClassIds ?? Array.Empty<string>();
            stats = row.Stats;
            price = row.Price;
            armoryLevel = row.ArmoryLevel;
            unlockRank = row.UnlockRank;
            twoHanded = row.TwoHanded;
        }
#endif
    }
}