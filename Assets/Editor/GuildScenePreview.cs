// Edit-mode Game-view preview for the Game scene: frame the camera at its start pose; hide/show the panel documents.
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using WanderersGuild;

namespace Editor
{
    public static class GuildScenePreview
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Preview/";
        private static bool _panelsHidden;

        // NOTE: Uses the same GuildCameraPose maths as Play mode. Undoable; marks the scene dirty (harmless — the rig sets the
        // pose itself at runtime).
        [MenuItem(MenuRoot + "Frame Start View")]
        private static void FrameStartView()
        {
            var rig = Object.FindAnyObjectByType<GuildCameraRig>();
            if (!rig)
            {
                Debug.LogWarning("[Preview] No GuildCameraRig in the open scenes.");
                return;
            }

            if (!TryReadConfig(out var camera, out var renderHeight))
            {
                Debug.LogWarning("[Preview] Couldn't find GameConfig (open scenes, prefabs or the Bootstrap scene).");
                return;
            }

            var (position, rotation) = rig.StartPose(camera, renderHeight);
            var cam = rig.GetComponent<Camera>();

            Undo.RecordObjects(new Object[] { rig.transform, cam }, "Frame Start View");
            rig.transform.SetPositionAndRotation(position, rotation);
            cam.fieldOfView = camera.FieldOfView;
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        }

        // NOTE: An Edit-mode style change only (never saved; a domain reload or Play resets it). The world view and the HUD stay.
        [MenuItem(MenuRoot + "Toggle Panels in Game View")]
        private static void TogglePanels()
        {
            _panelsHidden = !_panelsHidden;

            foreach (var document in Object.FindObjectsByType<UIDocument>())
            {
                var keep = document.GetComponent<WorldViewPresenter>() || document.GetComponent<GuildHudPresenter>();
                if (keep || document.rootVisualElement == null) continue;

                document.rootVisualElement.style.display = _panelsHidden ? DisplayStyle.None : DisplayStyle.Flex;
            }

            Debug.Log(_panelsHidden ? "[Preview] Panels hidden in the Game view." : "[Preview] Panels shown in the Game view.");
        }

        [MenuItem(MenuRoot + "Frame Start View", true)]
        [MenuItem(MenuRoot + "Toggle Panels in Game View", true)]
        private static bool CanPreview() => !EditorApplication.isPlayingOrWillChangePlaymode;

        // NOTE: GameConfig lives in the Bootstrap scene. Order: any open scene → a prefab → the Bootstrap scene opened
        // additively, read, and closed again without saving.
        private static bool TryReadConfig(out GuildCameraSettings camera, out int renderHeight)
        {
            camera = null;
            renderHeight = 180;

            var config = Object.FindAnyObjectByType<GameConfig>();
            if (!config)
                config = FindInPrefabs();
            if (config)
                return Read(config, out camera, out renderHeight);

            if (EditorBuildSettings.scenes.Length == 0)
                return false;

            var path = EditorBuildSettings.scenes[0].path;
            var bootstrap = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var root in bootstrap.GetRootGameObjects())
                {
                    config = root.GetComponentInChildren<GameConfig>(true);
                    if (config)
                        return Read(config, out camera, out renderHeight);
                }
                return false;
            }
            finally
            {
                EditorSceneManager.CloseScene(bootstrap, true);
            }
        }

        private static GameConfig FindInPrefabs()
        {
            return AssetDatabase.FindAssets("t:Prefab").Select(guid =>
                AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))).Select(prefab =>
                prefab ? prefab.GetComponentInChildren<GameConfig>(true) : null).FirstOrDefault(config => config);
        }

        // NOTE: A JSON copy, so the settings stay valid after the Bootstrap scene is closed again.
        private static bool Read(GameConfig config, out GuildCameraSettings camera, out int renderHeight)
        {
            camera = JsonUtility.FromJson<GuildCameraSettings>(JsonUtility.ToJson(config.GuildCamera));
            renderHeight = config.WorldView.RenderSize.y;
            return camera != null;
        }
    }
}