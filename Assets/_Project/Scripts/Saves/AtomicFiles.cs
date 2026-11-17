// Writes a file safely: to a temp file first, then swapped into place (replace on desktop, delete + move on the web), then
// flushed to the browser's storage on the web.
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
                Swap(tempPath, path);
                PlatformStorage.Flush();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AtomicFile] Couldn't write '{path}'. {exception.Message}");
                TryDeleteTemp(tempPath);
                return false;
            }
        }

        // NOTE: Desktop replaces in one step (a crash mid-write leaves the old file). The web file system lives in memory until
        // flushed, so delete + move there is just as safe.
        private static void Swap(string tempPath, string path)
        {
            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

            if (PlatformStorage.SupportsReplace)
            {
                File.Replace(tempPath, path, null);
                return;
            }

            File.Delete(path);
            File.Move(tempPath, path);
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