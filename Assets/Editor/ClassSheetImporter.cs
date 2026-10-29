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

        // NOTE: ICON_REPO isn't read yet — icons assigned by hand on the assets survive reloads.
        protected override ClassImportRow ReadRow(CsvRow row) => new(
            SheetCells.Text(row, "ID"),
            SheetCells.SingleLineText(row, "NAME"),
            SheetCells.MultilineText(row, "DESC"),
            SheetCells.Stats(row, "STATS"),
            SheetCells.Stats(row, "STAT_GROWTH"),
            SheetCells.EnumValue<GuildRank>(row, "UNLOCK"),
            SheetCells.List(row, "UPGRADES"),
            SheetCells.EnumList<QuestCategory>(row, "CATEGORIES"));

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