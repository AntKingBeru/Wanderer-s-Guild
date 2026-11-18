// Editor check: does every data text (requests, facilities, adventurer names) fit its UI box? Logs each one that doesn't.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WanderersGuild;
using Object = UnityEngine.Object;

namespace Editor
{
    // NOTE: Strategy pattern — each check knows one kind of data and the TextLimits boxes it fills; the validator runs them.
    public interface ITextLengthCheck
    {
        // Calls report(message, asset) for each text that doesn't fit; returns how many texts it checked.
        int Run(Action<string, Object> report);
    }

    public static class TextLengthValidator
    {
        private const string MenuPath = "Tools/Wanderer's Guild/Validate Text Lengths";
        private const int PreviewLength = 48;

        private static readonly ITextLengthCheck[] Checks =
        {
            new RequestTextCheck(),
            new FacilityTextCheck(),
            new NameTextCheck()
        };

        [MenuItem(MenuPath)]
        private static void ValidateFromMenu() => Validate();

        // Runs every check; true if everything fits. Also runs at the end of Database › Load.
        public static bool Validate()
        {
            var problems = 0;
            var checkedTexts = Checks.Sum(check => check.Run((message, asset) =>
            {
                problems++;
                Debug.LogWarning($"[Text] {message}", asset);
            }));

            if (problems == 0)
                Debug.Log($"[Text] All {checkedTexts} texts fit their boxes.");
            else
                Debug.LogWarning($"[Text] {problems} of {checkedTexts} texts don't fit their boxes (see above).");
            return problems == 0;
        }

        // Reports the text if it doesn't fit the box; owner names it in the message (an id or a list name).
        public static void Check(TextBox box, string text, string owner, Object asset, Action<string, Object> report)
        {
            if (box.Fits(text, out var used))
                return;
            report($"{owner}: {box.Label} uses {box.Usage(used)} — \"{Preview(text)}\"", asset);
        }

        public static IEnumerable<T> LoadAll<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).Where(asset => asset != null);
        }

        private static string Preview(string text)
        {
            var flat = (text ?? string.Empty).Replace("\r", string.Empty).Replace("\n", " / ");
            return flat.Length <= PreviewLength ? flat : flat[..(PreviewLength - 3)] + "...";
        }
    }
}