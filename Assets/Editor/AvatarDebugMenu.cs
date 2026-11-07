// Play-Mode debug menu for avatars: log who's doing what where, list the activity spots, respawn everyone.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class AvatarDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Avatars/";

        [MenuItem(MenuRoot + "Log Avatars")]
        private static void LogAvatars()
        {
            foreach (var avatar in AvatarManager.Instance.Avatars.Values)
            {
                var state = avatar.IsLeaving ? "leaving" : avatar.IsWalking ? "walking" : "standing";
                Debug.Log($"#{avatar.AdventurerId} at {avatar.Cell} — {avatar.PlannedFor}, {state}");
            }
            Debug.Log($"[Avatars] {AvatarManager.Instance.Avatars.Count} shown; nav grid {AvatarManager.Instance.Nav.CellCount} cells.");
        }

        [MenuItem(MenuRoot + "Log Activity Spots")]
        private static void LogSpots()
        {
            foreach (SpotKind kind in System.Enum.GetValues(typeof(SpotKind)))
                Debug.Log($"[Avatars] {kind}: {ActivitySpotRegistry.CountOf(kind)} spot(s)");
        }

        [MenuItem(MenuRoot + "Respawn All")]
        private static void RespawnAll() => AvatarManager.Instance.RespawnAll();

        [MenuItem(MenuRoot + "Log Avatars", true)]
        [MenuItem(MenuRoot + "Log Activity Spots", true)]
        [MenuItem(MenuRoot + "Respawn All", true)]
        private static bool CanUse() => EditorApplication.isPlaying && AvatarManager.HasInstance;
    }
}