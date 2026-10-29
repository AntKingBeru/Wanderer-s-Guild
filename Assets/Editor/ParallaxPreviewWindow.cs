using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WanderersGuild;

namespace Editor
{
    public class ParallaxPreviewWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Wanderer's Guild/Parallax Preview";
        private const float SweepSpeed = 1.5f;
        
        [SerializeField] private GameConfig configPrefab;
        
        private readonly Dictionary<ParallaxLayer, PreviewRecord> _records = new();
        private bool _previewing;
        private bool _autoSweep;
        private bool _layersDirty = true;
        private Vector2 _pointer;
        
        [MenuItem(MenuPath)]
        private static void Open() => GetWindow<ParallaxPreviewWindow>("Parallax Preview");
        
        private void OnEnable()
        {
            if (!configPrefab)
                configPrefab = FindConfigPrefab();

            EditorApplication.update += OnEditorUpdate;
            EditorApplication.hierarchyChanged += MarkLayersDirty;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
        }
        
        private void OnDisable()
        {
            StopPreview();

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.hierarchyChanged -= MarkLayersDirty;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
        }
        
        #region GUI
        private void OnGUI()
        {
            configPrefab = (GameConfig)EditorGUILayout.ObjectField("Game Config Prefab", configPrefab, typeof(GameConfig), false);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Edit Mode only. In Play Mode, just move the mouse.", MessageType.Info);
                return;
            }

            if (!configPrefab)
            {
                EditorGUILayout.HelpBox("Assign the GameConfig prefab to read the parallax settings.", MessageType.Warning);
                return;
            }

            var wantsPreview = GUILayout.Toggle(_previewing, _previewing ? "Stop Preview" : "Start Preview", "Button");
            if (wantsPreview != _previewing)
            {
                if (wantsPreview)
                    StartPreview();
                else
                    StopPreview();
            }

            _autoSweep = EditorGUILayout.Toggle("Auto Sweep (circle)", _autoSweep);

            using (new EditorGUI.DisabledScope(_autoSweep))
            {
                _pointer.x = EditorGUILayout.Slider("Pointer X", _pointer.x, -1f, 1f);
                _pointer.y = EditorGUILayout.Slider("Pointer Y", _pointer.y, -1f, 1f);
                if (GUILayout.Button("Center Pointer"))
                    _pointer = Vector2.zero;
            }

            EditorGUILayout.HelpBox(
                "Edit each layer's Depth in the Inspector to see it live.\n" +
                "Original positions are restored when the preview stops, before saving, and before Play Mode.",
                MessageType.None);
        }
        #endregion
        
        #region Preview Lifecycle
        private void StartPreview()
        {
            _previewing = true;
            _layersDirty = true;
        }

        private void StopPreview()
        {
            RestoreAll();
            _records.Clear();
            _previewing = false;
            RefreshViews();
        }
        
        private void OnEditorUpdate()
        {
            if (!_previewing)
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode || !configPrefab)
            {
                StopPreview();
                return;
            }

            if (_autoSweep)
            {
                var angle = (float)EditorApplication.timeSinceStartup * SweepSpeed;
                _pointer = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Repaint();
            }

            if (_layersDirty) RefreshLayers();

            var settings = configPrefab.Parallax;
            foreach (KeyValuePair<ParallaxLayer, PreviewRecord> pair in _records)
            {
                if (!pair.Key)
                    continue;
                var offset = ParallaxLayer.CalculateOffset(_pointer, pair.Key.Depth, settings);
                pair.Value.Apply(pair.Key.transform, offset);
            }

            RefreshViews();
        }

        private void RefreshLayers()
        {
            foreach (var layer in Object.FindObjectsByType<ParallaxLayer>())
            {
                if (!_records.ContainsKey(layer))
                    _records.Add(layer, new PreviewRecord(layer.transform.localPosition));
            }

            // Prune layers that were deleted or whose scene closed.
            List<ParallaxLayer> destroyed = new();
            foreach (var layer in _records.Keys)
            {
                if (!layer)
                    destroyed.Add(layer);
            }
            foreach (var layer in destroyed) _records.Remove(layer);

            _layersDirty = false;
        }

        private void RestoreAll()
        {
            foreach (KeyValuePair<ParallaxLayer, PreviewRecord> pair in _records)
            {
                if (pair.Key)
                    pair.Value.Restore(pair.Key.transform);
            }
        }
        #endregion
        
        #region Editor Events
        private void MarkLayersDirty() => _layersDirty = true;

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                StopPreview();
        }
        
        private void OnSceneSaving(Scene scene, string path) => RestoreAll();

        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            RestoreAll();
            _layersDirty = true;
        }
        #endregion
        
        #region Helpers
        private static void RefreshViews()
        {
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
        
        private static GameConfig FindConfigPrefab()
        {
            foreach (var guid in AssetDatabase.FindAssets("GameConfig t:Prefab"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab && prefab.TryGetComponent(out GameConfig config))
                    return config;
            }
            return null;
        }
        
        private sealed class PreviewRecord
        {
            private Vector3 _rest;
            private Vector3 _lastOffset;
            private Vector3 _lastApplied;

            public PreviewRecord(Vector3 rest)
            {
                _rest = rest;
                _lastApplied = rest;
                _lastOffset = Vector3.zero;
            }

            public void Apply(Transform target, Vector3 offset)
            {
                SyncRest(target);
                _lastOffset = offset;
                _lastApplied = _rest + offset;
                target.localPosition = _lastApplied;
            }

            public void Restore(Transform target)
            {
                SyncRest(target);
                _lastOffset = Vector3.zero;
                _lastApplied = _rest;
                target.localPosition = _rest;
            }
            
            private void SyncRest(Transform target)
            {
                if (target.localPosition != _lastApplied)
                    _rest = target.localPosition - _lastOffset;
            }
        }
        #endregion
    }
}