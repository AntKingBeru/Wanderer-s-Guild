// How many objects want the pixel outline right now; the renderer feature skips its passes while this is 0.
using UnityEngine;

namespace WanderersGuild
{
    public static class PixelOutlineRequests
    {
        public static int Count { get; private set; }

        public static void Add() => Count++;
        public static void Remove() => Count = Mathf.Max(0, Count - 1);

        // NOTE: With domain reload disabled, a stale count from the last Play session would keep the passes running.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Count = 0;
    }
}