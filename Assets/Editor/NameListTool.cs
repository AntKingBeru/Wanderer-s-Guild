// Creates a filled name list for each default species (Human, Elf, Dwarf) and assigns it to that species' asset.
using System.Linq;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class NameListTool
    {
        private const string Parent = "Assets/_Project/ScriptableObjects";
        private const string Folder = Parent + "/Names";
        private const string MenuPath = "Tools/Wanderer's Guild/Adventurers/Create Species Name Lists";

        // NOTE: Never overwrites — an existing list asset is kept as is, and a species that already has a list keeps it.
        [MenuItem(MenuPath)]
        private static void CreateSpeciesNameLists()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder(Parent, "Names");

            int created = 0, assigned = 0;
            foreach (var defaults in DefaultAdventurerNames.All)
            {
                var path = $"{Folder}/{defaults.AssetName}.asset";
                var list = AssetDatabase.LoadAssetAtPath<AdventurerNameList>(path);
                if (list == null)
                {
                    list = ScriptableObject.CreateInstance<AdventurerNameList>();
                    defaults.ApplyTo(list); // Filled before it's saved.
                    AssetDatabase.CreateAsset(list, path);
                    created++;
                }

                var species = FindSpecies(defaults.SpeciesId);
                if (species == null)
                {
                    Debug.LogWarning($"[Names] No species '{defaults.SpeciesId}' — {defaults.AssetName} left unassigned.");
                    continue;
                }
                if (species.NameList != null)
                    continue;

                species.SetNameList(list);
                EditorUtility.SetDirty(species);
                assigned++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Names] {created} list(s) created, {assigned} species assigned (existing lists and assignments kept).");
        }

        [MenuItem(MenuPath, true)]
        private static bool CanUse() => !EditorApplication.isPlaying;

        private static SpeciesData FindSpecies(string id)
        {
            return AssetDatabase.FindAssets("t:SpeciesData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<SpeciesData>(AssetDatabase.GUIDToAssetPath(guid))).
                FirstOrDefault(species => species != null && species.Id == id);
        }
    }
}