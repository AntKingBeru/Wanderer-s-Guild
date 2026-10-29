// Mirrors the game speed onto Time.timeScale (0 when paused) so animations and physics follow; UI runs on unscaled time.
using UnityEngine;

namespace WanderersGuild
{
    public static class TimeScaleSync
    {
        public static void Apply(float multiplier) => Time.timeScale = Mathf.Max(0f, multiplier);

        // Back to normal — called when the Game scene ends so menus never inherit a paused or fast scale.
        public static void Reset() => Time.timeScale = 1f;
    }
}