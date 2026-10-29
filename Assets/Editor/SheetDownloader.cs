// Downloads one tab of the Google Sheet as CSV bytes without blocking the editor; null on failure (logged).
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Editor
{
    public static class SheetDownloader
    {
        public static async Task<byte[]> DownloadCsvAsync(string sheetName)
        {
            var url = $"{DatabaseImportConfig.SheetUrl}&sheet={UnityWebRequest.EscapeURL(sheetName)}";

            using var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            // NOTE: Yields to the editor loop each step, so the editor stays responsive while downloading.
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
                return request.downloadHandler.data;

            Debug.LogError($"[Database] Couldn't download sheet '{sheetName}': {request.error}");
            return null;
        }
    }
}