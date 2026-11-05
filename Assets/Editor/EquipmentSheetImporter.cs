// Imports the "Equipment" sheet into EquipmentData assets + EquipmentDatabase; warns about class ids that don't exist.
using System;
using System.Collections.Generic;
using Csv;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public sealed class EquipmentSheetImporter : SheetImporter<EquipmentImportRow, EquipmentData, EquipmentDatabase>
    {
        private const string TwoHandedColumn = "TWO_HANDED"; // Optional; empty = false.

        private HashSet<string> _classIds;

        protected override string SheetName => "Equipment";
        protected override string DatabaseName => "EquipmentDatabase";

        protected override string IdOf(in EquipmentImportRow row) => row.Id;
        protected override void Apply(EquipmentData asset, in EquipmentImportRow row) => asset.ApplyImport(row);
        protected override void SetDatabase(EquipmentDatabase database, EquipmentData[] assets) => database.SetItems(assets);

        // NOTE: ARMORY_LEVEL 0 = starting gear only (never sold). ICON_REPO isn't read. Unknown class ids are kept (they just
        // never match) but warned, so typos show up.
        protected override EquipmentImportRow ReadRow(CsvRow row)
        {
            var price = SheetCells.WholeNumber(row, "PRICE");
            var armoryLevel = SheetCells.WholeNumber(row, "ARMORY_LEVEL");
            if (price < 0)
                throw new FormatException("PRICE can't be negative");
            if (armoryLevel < 0)
                throw new FormatException("ARMORY_LEVEL can't be negative (0 = starting gear only)");

            var id = SheetCells.Text(row, "ID");
            var classes = SheetCells.List(row, "CLASSES");
            WarnUnknownClasses(id, classes);

            var twoHanded = SheetCells.TryText(row, TwoHandedColumn, out _) && SheetCells.Flag(row, TwoHandedColumn);

            return new EquipmentImportRow(
                id,
                SheetCells.SingleLineText(row, "NAME"),
                SheetCells.MultilineText(row, "DESC"),
                SheetCells.EnumValue<EquipmentSlot>(row, "SLOT"),
                classes,
                SheetCells.Stats(row, "STATS"),
                price,
                armoryLevel,
                SheetCells.EnumValue<GuildRank>(row, "UNLOCK"),
                twoHanded);
        }

        private void WarnUnknownClasses(string itemId, string[] classes)
        {
            _classIds ??= LoadClassIds();
            foreach (var classId in classes)
            {
                if (!_classIds.Contains(classId))
                    Debug.LogWarning($"[Equipment] '{itemId}' lists unknown class '{classId}' (import Classes first, or fix the id).");
            }
        }

        private static HashSet<string> LoadClassIds()
        {
            HashSet<string> ids = new();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ClassData"))
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<ClassData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (data && !string.IsNullOrEmpty(data.Id))
                    ids.Add(data.Id);
            }
            return ids;
        }
    }
}