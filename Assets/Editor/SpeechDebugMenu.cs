// Play-Mode debug menu for speech bubbles: everyone chatters now, or everyone says a random event line.
using UnityEditor;
using WanderersGuild;
using Random = UnityEngine.Random;

namespace Editor
{
    public static class SpeechDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Avatars/Speech/";

        [MenuItem(MenuRoot + "Everyone Chatters")]
        private static void EveryoneChatters()
        {
            foreach (var avatar in AvatarManager.Instance.Avatars.Values)
                SpeechBubbleDirector.Instance.Say(avatar.AdventurerId, SpeechTopic.Chatter);
        }

        // NOTE: Event topics are Departing … WellFed (enum order).
        [MenuItem(MenuRoot + "Everyone Says An Event Line")]
        private static void EveryoneSaysEvent()
        {
            foreach (var avatar in AvatarManager.Instance.Avatars.Values)
            {
                var topic = (SpeechTopic)Random.Range(0, (int)SpeechTopic.WellFed + 1);
                SpeechBubbleDirector.Instance.Say(avatar.AdventurerId, topic);
            }
        }

        [MenuItem(MenuRoot + "Everyone Chatters", true)]
        [MenuItem(MenuRoot + "Everyone Says An Event Line", true)]
        private static bool CanUse() =>
            EditorApplication.isPlaying && AvatarManager.HasInstance && SpeechBubbleDirector.HasInstance;
    }
}