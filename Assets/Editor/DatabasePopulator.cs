// Editor tool: scans the project for each data-asset type and writes them into their database SO.
#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class DatabasePopulator
    {
        [MenuItem("Wanderer's Guild/Data/Rebuild All Databases")]
        public static void RebuildAll()
        {
            Populate<SpeciesData, SpeciesDatabase>();
            Populate<ClassData, ClassDatabase>();
            Populate<RequestData, RequestDatabase>();
            AssetDatabase.SaveAssets();
            Debug.Log("[DatabasePopulator] All databases rebuilt.");
        }

        // NOTE: Finds every TAsset in the project and writes them into the single TDatabase asset.
        private static void Populate<TAsset, TDatabase>()
            where TAsset : ScriptableObject, IIdentifiable
            where TDatabase : ScriptableObjectDatabase<TAsset>
        {
            var dbGuids = AssetDatabase.FindAssets($"t:{typeof(TDatabase).Name}");
            if (dbGuids.Length == 0)
            {
                Debug.LogWarning($"[DatabasePopulator] No {typeof(TDatabase).Name} asset found; skipped.");
                return;
            }
            var database = AssetDatabase.LoadAssetAtPath<TDatabase>(AssetDatabase.GUIDToAssetPath(dbGuids[0]));

            var assetGuids = AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}");
            var list = new List<TAsset>(assetGuids.Length);
            list.AddRange(assetGuids.Select(guid => AssetDatabase.LoadAssetAtPath<TAsset>(AssetDatabase.GUIDToAssetPath(guid))).Where(asset => asset));

            database.EditorSetEntries(list);
            Debug.Log($"[DatabasePopulator] {typeof(TDatabase).Name}: {list.Count} entries.");
        }
    }
}
#endif