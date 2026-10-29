using System;
using System.IO;
using UnityEngine;

namespace WanderersGuild
{
    public static class AtomicFile
    {
        public static bool TryWriteText(string path, string contents) =>
            TryWrite(path, temp => File.WriteAllText(temp, contents));

        public static bool TryWriteBytes(string path, byte[] bytes) =>
            TryWrite(path, temp => File.WriteAllBytes(temp, bytes));

        // NOTE: Creates the folder if needed; the temp file is cleaned up on failure.
        private static bool TryWrite(string path, Action<string> write)
        {
            var tempPath = path + ".tmp";

            try
            {
                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);

                write(tempPath);

                if (File.Exists(path))
                    File.Replace(tempPath, path, null);
                else
                    File.Move(tempPath, path);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AtomicFile] Couldn't write '{path}'. {exception.Message}");
                TryDeleteTemp(tempPath);
                return false;
            }
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception)
            {
                // A leftover temp file is harmless; the next write overwrites it.
            }
        }
    }
}