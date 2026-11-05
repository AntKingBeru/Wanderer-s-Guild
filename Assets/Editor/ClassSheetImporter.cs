// Imports the "Classes" sheet into ClassData assets + ClassDatabase; upgrade ids are checked against the other rows.
using System.Collections.Generic;
using Csv;
using WanderersGuild;

namespace Editor
{
    public sealed class ClassSheetImporter : SheetImporter<ClassImportRow, ClassData, ClassDatabase>
    {
        protected override string SheetName => "Classes";
        protected override string DatabaseName => "ClassDatabase";

        protected override string IdOf(in ClassImportRow row) => row.Id;
        protected override void Apply(ClassData asset, in ClassImportRow row) => asset.ApplyImport(row);
        protected override void SetDatabase(ClassDatabase database, ClassData[] assets) => database.SetClasses(assets);

        // NOTE: ICON_REPO isn't read yet. MAX_RANK and STARTING_GEAR are optional (absent/empty = no cap, no gear).
        // STARTING_GEAR ids can't be checked here (Equipment imports after Classes) — the game warns about unknown ones.
        protected override ClassImportRow ReadRow(CsvRow row)
        {
            var maxRank = SheetCells.TryText(row, "MAX_RANK", out var max) && max.Length > 0
                ? SheetCells.EnumValue<GuildRank>(row, "MAX_RANK")
                : GuildRank.National;
            var gear = SheetCells.TryText(row, "STARTING_GEAR", out _) ? SheetCells.List(row, "STARTING_GEAR") : System.Array.Empty<string>();

            return new ClassImportRow(
                SheetCells.Text(row, "ID"),
                SheetCells.SingleLineText(row, "NAME"),
                SheetCells.MultilineText(row, "DESC"),
                SheetCells.Stats(row, "STATS"),
                SheetCells.Stats(row, "STAT_GROWTH"),
                SheetCells.EnumValue<GuildRank>(row, "UNLOCK"),
                maxRank,
                SheetCells.List(row, "UPGRADES"),
                SheetCells.EnumList<QuestCategory>(row, "CATEGORIES"),
                gear);
        }

        // NOTE: Upgrade ids must name another imported class — unknown ids and self-references are dropped with a warning.
        protected override List<ClassImportRow> ValidateAll(List<ClassImportRow> rows)
        {
            HashSet<string> ids = new();
            foreach (var row in rows)
                ids.Add(row.Id);

            List<ClassImportRow> result = new(rows.Count);
            foreach (var row in rows)
            {
                List<string> upgrades = new();
                foreach (var upgrade in row.AdvancesToIds)
                {
                    if (upgrade == row.Id || !ids.Contains(upgrade))
                        Warn($"'{row.Id}' upgrade '{upgrade}' isn't another class in the sheet; ignored.");
                    else
                        upgrades.Add(upgrade);
                }

                result.Add(upgrades.Count == row.AdvancesToIds.Length ? row : row.WithAdvancesTo(upgrades.ToArray()));
            }
            return result;
        }
    }
}