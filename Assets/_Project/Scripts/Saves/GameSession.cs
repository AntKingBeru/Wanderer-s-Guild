using UnityEngine;

namespace WanderersGuild
{
    public static class GameSession
    {
        // The slot every save of this game writes to; −1 when no game is running.
        public static int ActiveSlot { get; private set; } = -1;

        // True for a fresh guild (its slot may still hold the overwritten guild until the first save).
        public static bool IsNewGame { get; private set; }

        public static bool HasActiveSlot => ActiveSlot >= 0;

        public static void BeginNewGame(int slot)
        {
            ActiveSlot = slot;
            IsNewGame = true;
        }

        public static void BeginLoadedGame(int slot)
        {
            ActiveSlot = slot;
            IsNewGame = false;
        }

        public static void End()
        {
            ActiveSlot = -1;
            IsNewGame = false;
        }

        // NOTE: With domain reload disabled, statics would otherwise carry over between Play sessions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => End();
    }
}