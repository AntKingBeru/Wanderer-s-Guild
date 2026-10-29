// Play-Mode debug menu for requests: open the board, run a batch now, log the board, dismiss the oldest request.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class RequestDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Requests/";

        // NOTE: Stand-in until the in-world guild board exists; it will call the same GamePanelHost.Open.
        [MenuItem(MenuRoot + "Open Board")]
        private static void OpenBoard()
        {
            var host = Object.FindAnyObjectByType<GamePanelHost>();
            if (!host || host.IsBusy || host.HasOpenPanel)
            {
                Debug.LogWarning("[Requests] No idle GamePanelHost in the scene (close any open panel first).");
                return;
            }
            host.Open(MenuPanelId.RequestBoard);
        }

        [MenuItem(MenuRoot + "Generate Batch Now")]
        private static void Generate() => RequestManager.Instance.GenerateBatchNow();

        [MenuItem(MenuRoot + "Log Board")]
        private static void LogBoard()
        {
            var now = RequestManager.NowMinutes;
            foreach (var request in RequestManager.Instance.Requests)
            {
                var data = request.Data;
                Debug.Log($"#{request.InstanceId} \"{data.DisplayName}\" [{data.Category}] {data.Source}, rank {data.RecommendedRank}, " +
                          $"party {data.RecommendedPartySize}, {data.Reward.Gold} gold, " +
                          $"{RequestFormatter.TimeLeft(request.MinutesLeft(now))} left{(request.IsSeen ? "" : ", NEW")} — {data.Objective}");
            }
            Debug.Log($"[Requests] {RequestManager.Instance.Requests.Count} open.");
        }

        [MenuItem(MenuRoot + "Dismiss Oldest")]
        private static void DismissOldest()
        {
            if (RequestManager.Instance.Requests.Count > 0)
                RequestManager.Instance.Dismiss(RequestManager.Instance.Requests[0].InstanceId);
        }

        [MenuItem(MenuRoot + "Open Board", true)]
        [MenuItem(MenuRoot + "Generate Batch Now", true)]
        [MenuItem(MenuRoot + "Log Board", true)]
        [MenuItem(MenuRoot + "Dismiss Oldest", true)]
        private static bool CanUse() => EditorApplication.isPlaying && RequestManager.HasInstance;
    }
}