// Disk access for save slots: one folder per slot holding its header and thumbnail; reads never throw.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class SaveSlotStore
    {
        private readonly SaveSlotSettings _settings;
        private readonly HashSet<int> _reportedCorrupted = new(); // NOTE: Warn once per slot, not on every refresh.

        public SaveSlotStore(SaveSlotSettings settings) => _settings = settings;

        public int SlotCount => _settings.SlotCount;
        public string RootPath => Path.Combine(Application.persistentDataPath, _settings.RootFolder);

        // NOTE: Folders are numbered from 1 so they match what players would call them.
        public string SlotPath(int index) => Path.Combine(RootPath, $"slot_{index + 1}");

        private string HeaderPath(int index) => Path.Combine(SlotPath(index), _settings.HeaderFileName);
        private string ThumbnailPath(int index) => Path.Combine(SlotPath(index), _settings.ThumbnailFileName);

        // No header = Empty; an unreadable or invalid header = Corrupted.
        public SaveSlotInfo ReadSlot(int index)
        {
            var path = HeaderPath(index);
            if (!File.Exists(path))
            {
                _reportedCorrupted.Remove(index);
                return new SaveSlotInfo(index, SaveSlotState.Empty, null);
            }

            string reason;
            try
            {
                var header = JsonUtility.FromJson<SaveSlotHeader>(File.ReadAllText(path));
                if (header is { IsValid: true })
                {
                    _reportedCorrupted.Remove(index);
                    return new SaveSlotInfo(index, SaveSlotState.Ready, header);
                }

                reason = "it has missing or out-of-range values";
            }
            catch (Exception exception)
            {
                reason = exception.Message;
            }

            ReportCorrupted(index, reason);
            return new SaveSlotInfo(index, SaveSlotState.Corrupted, null);
        }

        public bool TryWriteHeader(int index, SaveSlotHeader header) =>
            header != null && AtomicFile.TryWriteText(HeaderPath(index), JsonUtility.ToJson(header, true));

        public bool TryWriteThumbnail(int index, Texture2D thumbnail)
        {
            if (!thumbnail)
                return false;

            var png = thumbnail.EncodeToPNG();
            return png != null && AtomicFile.TryWriteBytes(ThumbnailPath(index), png);
        }
        
        // Replaces whatever the slot held with a fresh header (a new guild).
        // NOTE: The header is written first (atomic, replacing the old one); only then is everything else in the folder
        // removed. A failed write therefore leaves the old guild untouched.
        public bool TryResetSlot(int index, SaveSlotHeader header)
        {
            if (!TryWriteHeader(index, header))
                return false;

            var folder = SlotPath(index);
            var keep = Path.GetFullPath(HeaderPath(index));

            try
            {
                foreach (var file in Directory.GetFiles(folder))
                {
                    if (!string.Equals(Path.GetFullPath(file), keep, StringComparison.OrdinalIgnoreCase))
                        File.Delete(file);
                }

                foreach (var directory in Directory.GetDirectories(folder))
                    Directory.Delete(directory, true);
            }
            catch (Exception exception)
            {
                // The new header is saved; leftovers (e.g. the old thumbnail) are cosmetic and replaced by later saves.
                Debug.LogWarning($"[SaveSlotStore] Slot {index + 1}: couldn't remove the previous guild's files. {exception.Message}");
            }

            _reportedCorrupted.Remove(index);
            return true;
        }

        // Runtime only. The caller owns the texture and must Destroy it. Null when missing or unreadable.
        public Texture2D LoadThumbnail(int index)
        {
            var path = ThumbnailPath(index);
            if (!File.Exists(path)) return null;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSlotStore] Slot {index + 1} thumbnail is unreadable. {exception.Message}");
                return null;
            }

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false) { name = $"SaveThumbnail_{index + 1}" };
            if (!texture.LoadImage(bytes, true))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            // NOTE: Set after LoadImage, which rebuilds the texture; point filtering keeps the pixel art crisp.
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        // Deletes the slot's whole folder (header, thumbnail and, later, game data).
        public bool TryDelete(int index)
        {
            var path = SlotPath(index);

            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSlotStore] Couldn't delete slot {index + 1}. {exception.Message}");
                return false;
            }
        }

        private void ReportCorrupted(int index, string reason)
        {
            if (_reportedCorrupted.Add(index))
                Debug.LogWarning($"[SaveSlotStore] Slot {index + 1} save header can't be used ({reason}). Shown as Unreadable Save.");
        }
    }
}