// Imports the "Species" sheet into SpeciesData assets + SpeciesDatabase.
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

        // NOTE: ICON_REPO isn't read yet — icons assigned by hand on the assets survive reloads.
        protected override SpeciesImportRow ReadRow(CsvRow row) => new(
            SheetCells.Text(row, "ID"),
            SheetCells.SingleLineText(row, "NAME"),
            SheetCells.MultilineText(row, "DESC"),
            SheetCells.Stats(row, "STATS"));
    }
}