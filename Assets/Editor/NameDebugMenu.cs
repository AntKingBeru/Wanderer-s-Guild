// Play-Mode debug: logs 12 sample names per species as if they all joined one roster (shows variety and repeats).
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class NameDebugMenu
    {
        private const string MenuPath = "Tools/Wanderer's Guild/Debug/Adventurers/Log Sample Names";
        private const int PerSpecies = 12;

        [MenuItem(MenuPath)]
        private static void LogSampleNames()
        {
            var settings = GameConfig.Instance.Adventurers;
            var generator = new AdventurerNameGenerator(settings.NameList, settings.MaxNameLength, settings.RecentNameMemory);
            var taken = new HashSet<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:SpeciesData"))
            {
                var species = AssetDatabase.LoadAssetAtPath<SpeciesData>(AssetDatabase.GUIDToAssetPath(guid));
                if (species == null)
                    continue;

                List<string> names = new(PerSpecies);
                for (var i = 0; i < PerSpecies; i++)
                {
                    var name = generator.Next(species, taken);
                    taken.Add(name);
                    names.Add(name);
                }

                var source = species.NameList != null ? species.NameList.name : "common list";
                Debug.Log($"[Names] {species.DisplayName} ({source}): {string.Join(", ", names)}");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool CanUse() => EditorApplication.isPlaying && GameConfig.HasInstance;
    }
}