// Play-Mode debug menu for the tutorial: skip / complete it now, log its stage, and log placed rooms (to fill Tutorial › Placements).
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class TutorialDebugMenu
    {
        private const string Root = "Tools/Wanderer's Guild/Debug/Tutorial/";

        [MenuItem(Root + "Skip Now (Build Start State)")]
        private static void SkipNow() => TutorialManager.Instance.Skip();

        [MenuItem(Root + "Complete Now")]
        private static void CompleteNow() => TutorialManager.Instance.Complete();

        [MenuItem(Root + "Log Stage")]
        private static void LogStage() => Debug.Log($"[Tutorial] Stage: {TutorialManager.Instance.Stage}");

        // NOTE: Build the tutorial's rooms by hand once, then copy these into GameConfig › Tutorial › Placements (in order).
        [MenuItem(Root + "Log Placed Rooms")]
        private static void LogPlacedRooms()
        {
            foreach (var room in FacilityManager.Instance.Rooms)
                Debug.Log($"[Tutorial] {room.Data.DisplayName} ({room.Data.Id}) — origin ({room.Origin.x}, {room.Origin.y}), " +
                          $"rotation {room.Rotation}");
        }

        [MenuItem(Root + "Skip Now (Build Start State)", true)]
        [MenuItem(Root + "Complete Now", true)]
        [MenuItem(Root + "Log Stage", true)]
        private static bool HasTutorial() => EditorApplication.isPlaying && TutorialManager.HasInstance;

        [MenuItem(Root + "Log Placed Rooms", true)]
        private static bool HasFacilities() => EditorApplication.isPlaying && FacilityManager.HasInstance;
    }
}