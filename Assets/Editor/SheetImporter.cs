// Base for importing one sheet: download, read rows (bad rows skipped), cross-row checks, then write assets in place by id
// plus the sheet's database asset (Template Method — subclasses only read and apply their own rows).
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Csv;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public abstract class SheetImporter<TRow, TAsset, TDatabase>
        where TAsset : ScriptableObject
        where TDatabase : ScriptableObject
    {
        // The sheet tab's name — also the item folder's name.
        protected abstract string SheetName { get; }
        protected abstract string DatabaseName { get; }

        protected abstract string IdOf(in TRow row);

        // Reads one row; throw FormatException with a clear message to skip it.
        protected abstract TRow ReadRow(CsvRow row);

        protected abstract void Apply(TAsset asset, in TRow row);
        protected abstract void SetDatabase(TDatabase database, TAsset[] assets);

        // Resets per-import state (e.g. "warned once" flags). Runs before reading.
        protected virtual void BeginRead() { }

        // Checks that need every row (e.g. references between rows). Returns the rows to write.
        protected virtual List<TRow> ValidateAll(List<TRow> rows) => rows;

        // False if the download failed (existing assets are left untouched).
        public async Task<bool> ImportAsync()
        {
            var csv = await SheetDownloader.DownloadCsvAsync(SheetName);
            if (csv == null)
                return false;

            BeginRead();
            var rows = ValidateAll(ReadRows(csv));
            WriteAssets(rows);
            return true;
        }

        // Deletes this sheet's generated assets (Database › Clean).
        public void DeleteGenerated()
        {
            AssetDatabase.DeleteAsset(DatabaseImportConfig.ItemFolder(SheetName));
            AssetDatabase.DeleteAsset(DatabaseImportConfig.DatabasePath(DatabaseName));
        }

        protected void Warn(string message) => Debug.LogWarning($"[Database] {SheetName}: {message}");

        // NOTE: Each row is read on its own — a bad row is skipped with its line number instead of aborting the import.
        private List<TRow> ReadRows(byte[] csv)
        {
            List<TRow> rows = new();
            HashSet<string> ids = new();
            var line = 1; // Header.

            foreach (var row in CsvSerializer.ConvertToDocument(csv).Rows)
            {
                line++;
                try
                {
                    var parsed = ReadRow(row);
                    var id = IdOf(parsed);
                    if (!ImportAssets.IsValidId(id))
                        throw new FormatException($"ID '{id}' is empty or can't be a file name");
                    if (!ids.Add(id))
                        throw new FormatException($"duplicate ID '{id}'");

                    rows.Add(parsed);
                }
                catch (Exception exception)
                {
                    Warn($"line {line} skipped: {exception.Message}");
                }
            }

            return rows;
        }

        // NOTE: Folders first (outside the batch), then one asset-editing batch; StopAssetEditing always runs (finally),
        // or the AssetDatabase would stay stuck in batch mode after an exception.
        private void WriteAssets(List<TRow> rows)
        {
            var folder = DatabaseImportConfig.ItemFolder(SheetName);
            ImportAssets.EnsureFolder(folder);
            ImportAssets.EnsureFolder(DatabaseImportConfig.DatabasesFolder);

            HashSet<string> keepIds = new();
            foreach (var row in rows)
                keepIds.Add(IdOf(row));

            int removed;
            List<TAsset> assets = new(rows.Count);

            AssetDatabase.StartAssetEditing();
            try
            {
                removed = ImportAssets.DeleteStale<TAsset>(folder, keepIds);

                foreach (var row in rows)
                {
                    var asset = ImportAssets.LoadOrCreate<TAsset>($"{folder}/{IdOf(row)}.asset");
                    Apply(asset, row);
                    EditorUtility.SetDirty(asset);
                    assets.Add(asset);
                }

                var database = ImportAssets.LoadOrCreate<TDatabase>(DatabaseImportConfig.DatabasePath(DatabaseName));
                SetDatabase(database, assets.ToArray());
                EditorUtility.SetDirty(database);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Database] {SheetName}: {assets.Count} imported, {removed} removed.");
        }
    }
}