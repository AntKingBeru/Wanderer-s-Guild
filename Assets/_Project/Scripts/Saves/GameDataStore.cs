// Reads and writes a slot's game.json (the full game state); paths come from SaveSlotStore, writes are atomic.
using System;
using System.IO;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class GameDataStore
    {
        private readonly SaveSlotStore _slots;
        private readonly SaveSlotSettings _settings;

        public GameDataStore(SaveSlotStore slots, SaveSlotSettings settings)
        {
            _slots = slots;
            _settings = settings;
        }

        private string DataPath(int index) => Path.Combine(_slots.SlotPath(index), _settings.GameFileName);

        // False when the file is missing, unreadable or invalid (never throws).
        public bool TryRead(int index, out GameSaveData data)
        {
            data = null;
            var path = DataPath(index);
            if (!File.Exists(path)) return false;

            try
            {
                data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
                if (data is { IsValid: true })
                    return true;

                Debug.LogWarning($"[GameDataStore] Slot {index + 1} game data has invalid values; falling back to its header.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameDataStore] Slot {index + 1} game data is unreadable; falling back to its header. {exception.Message}");
            }

            data = null;
            return false;
        }

        public bool TryWrite(int index, GameSaveData data) =>
            data != null && AtomicFile.TryWriteText(DataPath(index), JsonUtility.ToJson(data, true));
    }
}