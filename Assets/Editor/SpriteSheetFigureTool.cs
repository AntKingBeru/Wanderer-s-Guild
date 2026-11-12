// Editor: keeps every Adventurer Sprite Sheet's figure bounds up to date — from the menu, and whenever a sheet or its texture
// is (re)imported.
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public class SpriteSheetFigureTool : AssetPostprocessor
    {
        [MenuItem("Tools/Wanderer's Guild/Avatars/Measure Sprite Sheets")]
        private static void MeasureAll()
        {
            var changed = Measure(_ => true);
            Debug.Log($"[SpriteSheetFigureTool] Measured sprite sheets — {changed} updated.");
        }

        // NOTE: Deferred: editing assets during an import is unsafe. Only sheets whose asset or texture was imported are
        // re-measured; unchanged values don't dirty the asset, so saving can't loop back into another import.
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var paths = new HashSet<string>();
            foreach (var path in imported)
            {
                if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    paths.Add(path);
            }
            if (paths.Count == 0)
                return;

            EditorApplication.delayCall += () => Measure(sheet =>
                paths.Contains(AssetDatabase.GetAssetPath(sheet)) ||
                (sheet.Texture != null && paths.Contains(AssetDatabase.GetAssetPath(sheet.Texture))));
        }

        private static int Measure(Func<AdventurerSpriteSheet, bool> include)
        {
            var changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AdventurerSpriteSheet"))
            {
                var sheet = AssetDatabase.LoadAssetAtPath<AdventurerSpriteSheet>(AssetDatabase.GUIDToAssetPath(guid));
                if (sheet == null || !include(sheet))
                    continue;
                if (!SpriteSheetFigureMeasure.TryMeasure(sheet.Texture, out var top, out var bottom))
                    continue;
                if (!sheet.SetFigure(top, bottom))
                    continue;

                EditorUtility.SetDirty(sheet);
                changed++;
            }

            if (changed > 0)
                AssetDatabase.SaveAssets();
            return changed;
        }
    }
}