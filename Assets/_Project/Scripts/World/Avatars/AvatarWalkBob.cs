// The walk-bob curve for avatars without walk frames: a hop per step plus a side-to-side sway and lean (a waddle).
using UnityEngine;

namespace WanderersGuild
{
    public static class AvatarWalkBob
    {
        // NOTE: One step = half a sine period. The hop (|sin|) peaks mid-step on every step; the sway and lean (signed sin)
        // peak on alternate sides — left step, right step. Lift and sway are rounded to whole pixels (crisp art); the lean is
        // smooth. Everything is 0 at t = 0, so a walk starts and a stop lands upright on the ground.
        public static AvatarBobPose Sample(float walkTime, float stepsPerSecond, AvatarBobSettings settings)
        {
            if (walkTime <= 0f || stepsPerSecond <= 0f)
                return default;

            var wave = Mathf.Sin(walkTime * stepsPerSecond * Mathf.PI);
            return new AvatarBobPose(
                Mathf.RoundToInt(settings.HeightPixels * Mathf.Abs(wave)),
                Mathf.RoundToInt(settings.SwayPixels * wave),
                settings.LeanDegrees * wave);
        }
    }
}