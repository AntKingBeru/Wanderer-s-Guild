// Editor-only settings for the database importer: sheet URL, output folders, and fallbacks for columns the sheet lacks.
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    // NOTE: GameConfig is a scene singleton that doesn't exist in Edit Mode, so the importer's settings live in this one
    // editor-only file instead.
    public static class DatabaseImportConfig
    {
        // The shared Google Sheet, exported as CSV (one tab per request, chosen with &sheet=).
        public const string SheetUrl =
            "https://docs.google.com/spreadsheets/d/1UL95bYO5wsV2xSk2RbQ6nnPScQNDRPmimu5gp8r75WQ/gviz/tq?tqx=out:csv";

        public const string DataRoot = "Assets/_Project/Data";
        public const string DatabasesFolder = DataRoot + "/Databases";

        // Folder for one sheet's item assets, e.g. Assets/_Project/Data/Requests.
        public static string ItemFolder(string sheetName) => $"{DataRoot}/{sheetName}";

        // Path of a sheet's database asset, e.g. Assets/_Project/Data/Databases/RequestDatabase.asset.
        public static string DatabasePath(string databaseName) => $"{DatabasesFolder}/{databaseName}.asset";

        // NOTE: Used until the Requests sheet has a PARTY_SIZE column. Index = GuildRank: F, E, D, C, B, A, S, National.
        private static readonly int[] PartySizeByRank = { 2, 2, 3, 3, 4, 4, 5, 5 };

        public static int DefaultPartySize(GuildRank rank) =>
            PartySizeByRank[Mathf.Clamp((int)rank, 0, PartySizeByRank.Length - 1)];
    }
}