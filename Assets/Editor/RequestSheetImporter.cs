// Imports the "Requests" sheet into RequestData assets + RequestDatabase. Objective may span lines; title is one line.
using System;
using Csv;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public sealed class RequestSheetImporter : SheetImporter<RequestImportRow, RequestData, RequestDatabase>
    {
        // NOTE: DIFFICULTY exists in the sheet but is ignored — difficulty is calculated (quest session).
        private const string PartySizeColumn = "PARTY_SIZE"; // Optional until the sheet has it.

        private bool _notedNoPartySize;

        protected override string SheetName => "Requests";
        protected override string DatabaseName => "RequestDatabase";

        protected override string IdOf(in RequestImportRow row) => row.Id;
        protected override void BeginRead() => _notedNoPartySize = false;
        protected override void Apply(RequestData asset, in RequestImportRow row) => asset.ApplyImport(row);
        protected override void SetDatabase(RequestDatabase database, RequestData[] assets) => database.SetRequests(assets);

        protected override RequestImportRow ReadRow(CsvRow row)
        {
            var rank = SheetCells.EnumValue<GuildRank>(row, "RANK");

            var gold = SheetCells.WholeNumber(row, "REWARD");
            if (gold < 0)
                throw new FormatException("REWARD can't be negative");

            var expirationDays = SheetCells.WholeNumber(row, "EXPIRATION");
            if (expirationDays < 1)
                throw new FormatException("EXPIRATION must be at least 1 day");

            return new RequestImportRow(
                SheetCells.Text(row, "ID"),
                SheetCells.SingleLineText(row, "NAME"),
                SheetCells.MultilineText(row, "OBJECTIVE"),
                SheetCells.EnumValue<QuestCategory>(row, "CATEGORY"),
                rank,
                SheetCells.EnumValue<RequestSource>(row, "SOURCE"),
                ReadPartySize(row, rank),
                new RewardData(gold),
                expirationDays);
        }

        // NOTE: Until the sheet has PARTY_SIZE, it's derived from the rank (one note per import, not per row).
        private int ReadPartySize(CsvRow row, GuildRank rank)
        {
            if (SheetCells.TryText(row, PartySizeColumn, out var text) && !string.IsNullOrEmpty(text))
                return Mathf.Clamp(SheetCells.WholeNumber(row, PartySizeColumn), 1, 7);

            if (!_notedNoPartySize)
            {
                Debug.Log($"[Database] {SheetName}: no {PartySizeColumn} value — party size derived from rank.");
                _notedNoPartySize = true;
            }
            return DatabaseImportConfig.DefaultPartySize(rank);
        }
    }
}