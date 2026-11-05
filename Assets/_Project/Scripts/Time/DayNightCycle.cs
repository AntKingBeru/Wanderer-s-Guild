// Day/night maths (pure): the sun's rotation, color, intensity, the ambient color and a 0–1 daylight value at a time of day.
using UnityEngine;

namespace WanderersGuild
{
    public static class DayNightCycle
    {
        /// <param name="dayFraction">0 = midnight, 0.5 = noon, → 1.</param>
        /// <param name="settings"></param>
        // NOTE: The yaw sweeps a full circle per day around the noon direction (east at dawn, west at dusk); the night part of
        // the sweep is the "moon". Daylight is the intensity normalized against the curve's own min/max.
        public static SunState Evaluate(float dayFraction, LightingSettings settings)
        {
            var t = Mathf.Repeat(dayFraction, 1f);

            var elevation = settings.SunElevation.Evaluate(t);
            var yaw = settings.NoonYaw + (t - 0.5f) * 360f;
            var rotation = Quaternion.Euler(elevation, yaw, 0f);

            var intensity = Mathf.Max(0f, settings.SunIntensity.Evaluate(t));
            var (min, max) = Range(settings.SunIntensity);
            var daylight = max > min ? Mathf.InverseLerp(min, max, intensity) : 1f;

            return new SunState(rotation, settings.SunColor.Evaluate(t), intensity, settings.AmbientColor.Evaluate(t), daylight);
        }

        private static (float min, float max) Range(AnimationCurve curve)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var key in curve.keys)
            {
                min = Mathf.Min(min, key.value);
                max = Mathf.Max(max, key.value);
            }
            return curve.length == 0 ? (0f, 1f) : (min, max);
        }
    }
}