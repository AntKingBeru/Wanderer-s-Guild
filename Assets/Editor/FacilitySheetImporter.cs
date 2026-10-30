// Imports the "Facilities" sheet into FacilityData assets + FacilityDatabase; per-level lists must match MAX_LEVEL.
using System;
using Csv;
using WanderersGuild;

namespace Editor
{
    public sealed class FacilitySheetImporter : SheetImporter<FacilityImportRow, FacilityData, FacilityDatabase>
    {
        private const string LimitColumn = "LIMIT"; // Optional; empty = unlimited.

        protected override string SheetName => "Facilities";
        protected override string DatabaseName => "FacilityDatabase";

        protected override string IdOf(in FacilityImportRow row) => row.Id;
        protected override void Apply(FacilityData asset, in FacilityImportRow row) => asset.ApplyImport(row);
        protected override void SetDatabase(FacilityDatabase database, FacilityData[] assets) => database.SetFacilities(assets);

        // NOTE: ICON_REPO isn't read; prefabs per level and the icon are assigned on the assets by hand (reloads keep them).
        protected override FacilityImportRow ReadRow(CsvRow row)
        {
            var maxLevel = SheetCells.WholeNumber(row, "MAX_LEVEL");
            if (maxLevel < 1)
                throw new FormatException("MAX_LEVEL must be at least 1");

            var costs = SheetCells.WholeNumberList(row, "COST");
            var days = SheetCells.WholeNumberList(row, "BUILD_DAYS");
            var unlocks = SheetCells.EnumList<GuildRank>(row, "UNLOCK");
            RequireLevels(costs.Length, maxLevel, "COST");
            RequireLevels(days.Length, maxLevel, "BUILD_DAYS");
            RequireLevels(unlocks.Length, maxLevel, "UNLOCK");

            var longCosts = new long[costs.Length];
            for (var i = 0; i < costs.Length; i++)
            {
                if (costs[i] < 0)
                    throw new FormatException("COST values can't be negative");
                if (days[i] < 0)
                    throw new FormatException("BUILD_DAYS values can't be negative");
                longCosts[i] = costs[i];
            }

            var limit = 0;
            if (SheetCells.TryText(row, LimitColumn, out var limitText) && limitText.Length > 0)
                limit = Math.Max(0, SheetCells.WholeNumber(row, LimitColumn));

            return new FacilityImportRow(
                SheetCells.Text(row, "ID"),
                SheetCells.SingleLineText(row, "NAME"),
                SheetCells.MultilineText(row, "DESC"),
                SheetCells.Size(row, "SIZE"),
                maxLevel, longCosts, days, unlocks, limit);
        }

        private static void RequireLevels(int count, int maxLevel, string column)
        {
            if (count != maxLevel)
                throw new FormatException($"{column} needs one value per level ({maxLevel}), got {count}");
        }
    }
}