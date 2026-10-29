// Turns real elapsed time into a whole number of fixed simulation ticks, capped per frame (no catch-up spiral).
using System;

namespace WanderersGuild
{
    public sealed class TickAccumulator
    {
        private double _pending;

        // Progress toward the next tick (0–1). Lets visuals interpolate smoothly between ticks.
        public float Fraction => (float)_pending;

        /// <param name="realDeltaSeconds"></param>
        /// <param name="ticksPerSecond">0 or less = paused; the partial progress is kept for when time resumes.</param>
        /// <param name="maxTicks"></param>
        public int Consume(float realDeltaSeconds, double ticksPerSecond, int maxTicks)
        {
            if (ticksPerSecond <= 0d)
                return 0;

            _pending += realDeltaSeconds * ticksPerSecond;
            var ticks = (int)Math.Floor(_pending);

            // NOTE: After a hitch, drop the backlog instead of running hundreds of ticks in one frame.
            if (ticks > maxTicks)
            {
                _pending = 0d;
                return maxTicks;
            }

            _pending -= ticks;
            return ticks;
        }

        public void Reset() => _pending = 0d;
    }
}