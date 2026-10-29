// Asset helpers for the importer: create folders, load-or-create assets in place (GUIDs survive), remove stale ones.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class ImportAssets
    {
        // Creates every missing folder along a forward-slash asset path. Call outside StartAssetEditing.
        public static void EnsureFolder(string assetFolder)
        {
            var parts = assetFolder.Split('/');
            var current = parts[0]; // "Assets"

            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // The asset at the path, or a new one created there. Existing assets are reused, keeping their GUID.
        public static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        // Deletes assets of type T directly in the folder whose file name (= id) isn't kept. Returns how many.
        public static int DeleteStale<T>(string folder, HashSet<string> keepIds) where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetDirectoryName(path)?.Replace('\\', '/') == folder)
                .Count(path => !keepIds.Contains(Path.GetFileNameWithoutExtension(path)) && AssetDatabase.DeleteAsset(path));
        }

        // True when the id can safely be a file name (it names the asset).
        public static bool IsValidId(string id) =>
            !string.IsNullOrEmpty(id) && id.Trim() == id && id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }
}