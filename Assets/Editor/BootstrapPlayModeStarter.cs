using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    [InitializeOnLoad]
    public static class BootstrapPlayModeStarter
    {
        private const string MenuPath = "Tools/Wanderer's Guild/Play From Bootstrap";
        private const string PrefKey = "WanderersGuild.PlayFromBootstrap";

        static BootstrapPlayModeStarter()
        {
            EditorApplication.delayCall += Apply;
            EditorBuildSettings.sceneListChanged += Apply; // Keep in sync when the Build Profile scene list changes.
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        // NOTE: Uses scene index 0 of the scene list, which must be Bootstrap.
        private static void Apply()
        {
            if (!Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                Debug.LogWarning("[BootstrapPlayModeStarter] The scene list is empty; add Bootstrap at index 0.");
                return;
            }

            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenes[0].path);
        }
    }
}