// One-shot instruction for the next MainMenu load: skip the title screen (used when returning from a game).
using UnityEngine;

namespace WanderersGuild
{
    public static class MainMenuEntry
    {
        private static bool _skipTitle;

        public static void RequestSkipTitle() => _skipTitle = true;

        // Returns the request and clears it, so it applies to one MainMenu load only.
        public static bool ConsumeSkipTitle()
        {
            var skip = _skipTitle;
            _skipTitle = false;
            return skip;
        }

        // NOTE: With domain reload disabled, a leftover request would otherwise skip the title in the next Play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => _skipTitle = false;
    }
}