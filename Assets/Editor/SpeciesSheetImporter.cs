// Imports the "Species" sheet into SpeciesData assets + SpeciesDatabase (base stats and growth per level).
using Csv;
using WanderersGuild;

namespace Editor
{
    public sealed class SpeciesSheetImporter : SheetImporter<SpeciesImportRow, SpeciesData, SpeciesDatabase>
    {
        protected override string SheetName => "Species";
        protected override string DatabaseName => "SpeciesDatabase";

        protected override string IdOf(in SpeciesImportRow row) => row.Id;
        protected override void Apply(SpeciesData asset, in SpeciesImportRow row) => asset.ApplyImport(row);
        protected override void SetDatabase(SpeciesDatabase database, SpeciesData[] assets) => database.SetSpecies(assets);

        // NOTE: ICON_REPO isn't read yet. GROWTH is optional (absent/empty = no growth beyond the class bonus).
        protected override SpeciesImportRow ReadRow(CsvRow row)
        {
            var growth = SheetCells.TryText(row, "GROWTH", out var text) && text.Length > 0
                ? SheetCells.Stats(row, "GROWTH")
                : default;

            return new SpeciesImportRow(
                SheetCells.Text(row, "ID"),
                SheetCells.SingleLineText(row, "NAME"),
                SheetCells.MultilineText(row, "DESC"),
                SheetCells.Stats(row, "STATS"),
                growth);
        }
    }
}