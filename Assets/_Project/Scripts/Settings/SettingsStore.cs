using System;
using System.IO;
using UnityEngine;

namespace WanderersGuild
{
    public static class SettingsStore
    {
        public static string PathFor(string fileName) => Path.Combine(Application.persistentDataPath, fileName);

        // False when the file is missing or unreadable — the caller then uses defaults.
        public static bool TryLoad(string fileName, out GameSettingsData data)
        {
            data = null;
            var path = PathFor(fileName);
            if (!File.Exists(path)) return false;

            try
            {
                data = JsonUtility.FromJson<GameSettingsData>(File.ReadAllText(path));
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SettingsStore] '{path}' is unreadable; using defaults. {exception.Message}");
                data = null;
                return false;
            }
        }

        public static bool TrySave(string fileName, GameSettingsData data) =>
            AtomicFile.TryWriteText(PathFor(fileName), JsonUtility.ToJson(data, true));
    }
}