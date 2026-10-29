// Editor menu for testing save slots without gameplay: adds fake saves, deletes all saves, opens the save folder.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using WanderersGuild;
using Random = UnityEngine.Random;

namespace Editor
{
    public static class SaveSlotDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Saves/";

        private static readonly string[] GuildNames =
        {
            "Silver Lantern", "Wolfsbane Company", "The Gilded Mug", "Ashen Road", "Brightwater"
        };

        [MenuItem(MenuRoot + "Add Test Save (Next Empty Slot)")]
        private static void AddTestSave()
        {
            if (!TryGetStore(out var store, out var settings))
                return;

            for (var i = 0; i < store.SlotCount; i++)
            {
                if (store.ReadSlot(i).State != SaveSlotState.Empty)
                    continue;

                WriteTestSave(store, settings, i);
                RefreshRuntime();
                return;
            }

            Debug.Log("[SaveSlotDebugMenu] Every slot is already used.");
        }

        [MenuItem(MenuRoot + "Fill All Slots")]
        private static void FillAllSlots()
        {
            if (!TryGetStore(out var store, out var settings))
                return;

            for (var i = 0; i < store.SlotCount; i++)
                WriteTestSave(store, settings, i);

            RefreshRuntime();
        }

        [MenuItem(MenuRoot + "Write Corrupted Save (Next Empty Slot)")]
        private static void WriteCorruptedSave()
        {
            if (!TryGetStore(out var store, out var settings))
                return;

            for (var i = 0; i < store.SlotCount; i++)
            {
                if (store.ReadSlot(i).State != SaveSlotState.Empty)
                    continue;

                var folder = store.SlotPath(i);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, settings.HeaderFileName), "{ not valid json");
                RefreshRuntime();
                return;
            }

            Debug.Log("[SaveSlotDebugMenu] Every slot is already used.");
        }

        [MenuItem(MenuRoot + "Delete All Saves")]
        private static void DeleteAllSaves()
        {
            if (!TryGetStore(out var store, out _))
                return;
            if (!EditorUtility.DisplayDialog("Delete All Saves", $"Delete every save slot in:\n{store.RootPath}?", "Delete", "Cancel"))
                return;

            for (var i = 0; i < store.SlotCount; i++)
                store.TryDelete(i);

            RefreshRuntime();
        }

        [MenuItem(MenuRoot + "Open Save Folder")]
        private static void OpenSaveFolder()
        {
            if (!TryGetStore(out var store, out _))
                return;

            Directory.CreateDirectory(store.RootPath);
            EditorUtility.RevealInFinder(store.RootPath);
        }

        private static void WriteTestSave(SaveSlotStore store, SaveSlotSettings settings, int index)
        {
            var rank = (GuildRank)Random.Range(0, Enum.GetValues(typeof(GuildRank)).Length);
            GameDateStamp date = new(Random.Range(1, 6), Random.Range(1, 13), Random.Range(1, 29));
            double playTime = Random.Range(600f, 360000f);
            var savedAt = DateTime.UtcNow.AddMinutes(-Random.Range(1, 60 * 24 * 14));

            var header = SaveSlotHeader.Create(GuildNames[index % GuildNames.Length], rank, date, playTime, savedAt);
            store.TryWriteHeader(index, header);

            var thumbnail = CreateTestThumbnail(settings.ThumbnailSize, Random.value);
            try
            {
                store.TryWriteThumbnail(index, thumbnail);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(thumbnail); // NOTE: Edit Mode — Destroy() isn't allowed here.
            }
        }

        // NOTE: Four horizontal bands of one hue — enough to check the frame, cropping and point filtering.
        private static Texture2D CreateTestThumbnail(Vector2Int size, float hue)
        {
            var width = Mathf.Max(1, size.x);
            var height = Mathf.Max(1, size.y);
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                var band = Mathf.Floor(y / (float)height * 4f) / 4f;
                var color = Color.HSVToRGB(hue, 0.5f, 0.3f + band * 0.5f);
                for (var x = 0; x < width; x++)
                    pixels[y * width + x] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // NOTE: GameConfig.Instance only exists in Play Mode; in Edit Mode the prefab's settings are used.
        private static bool TryGetStore(out SaveSlotStore store, out SaveSlotSettings settings)
        {
            var config = GameConfig.HasInstance ? GameConfig.Instance : FindConfigPrefab();
            if (!config)
            {
                Debug.LogError("[SaveSlotDebugMenu] No GameConfig prefab found.");
                store = null;
                settings = null;
                return false;
            }

            settings = config.SaveSlots;
            store = new SaveSlotStore(settings);
            return true;
        }

        private static void RefreshRuntime()
        {
            if (Application.isPlaying && SaveSlotManager.HasInstance)
                SaveSlotManager.Instance.Refresh();
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
    }
}