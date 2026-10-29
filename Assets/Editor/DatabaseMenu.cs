// Editor menu for the data sheets: Database › Load imports every sheet; Database › Clean deletes the generated data.
using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class DatabaseMenu
    {
        private static bool _isLoading;

        // NOTE: Sheets import one after another; the refresh runs once at the end.
        [MenuItem("Database/Load")]
        private static async void Load()
        {
            if (_isLoading)
                return;
            _isLoading = true;

            try
            {
                var species = await new SpeciesSheetImporter().ImportAsync();
                var classes = await new ClassSheetImporter().ImportAsync();
                var requests = await new RequestSheetImporter().ImportAsync();

                Debug.Log(species && classes && requests
                    ? "[Database] Load finished."
                    : "[Database] Load finished with errors (see above).");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isLoading = false;
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Database/Load", true)]
        private static bool CanLoad() => !_isLoading && !EditorApplication.isPlayingOrWillChangePlaymode;

        // NOTE: Deleting changes GUIDs — references in GameConfig (Requests › Database, Adventurers › Class/Species Database)
        // must be reassigned after a Clean.
        [MenuItem("Database/Clean")]
        private static void Clean()
        {
            if (!EditorUtility.DisplayDialog("Clean database",
                    "Delete every imported data asset (species, classes, requests and their databases)?\n\n" +
                    "References to them in GameConfig will need reassigning after the next Load.",
                    "Delete", "Cancel"))
                return;

            new SpeciesSheetImporter().DeleteGenerated();
            new ClassSheetImporter().DeleteGenerated();
            new RequestSheetImporter().DeleteGenerated();
            AssetDatabase.Refresh();
        }

        [MenuItem("Database/Clean", true)]
        private static bool CanClean() => !_isLoading && !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}