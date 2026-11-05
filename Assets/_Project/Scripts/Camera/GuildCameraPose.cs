// The guild camera's pose maths (shared by the rig at runtime and the editor preview): orbit position, tilt, pixel snap, FOV.
using UnityEngine;

namespace WanderersGuild
{
    public static class GuildCameraPose
    {
        // Distance from the focus at this zoom (0 = far, 1 = near).
        public static float Distance(float zoom01, GuildCameraSettings settings) =>
            Mathf.Lerp(settings.FarDistance, settings.NearDistance, zoom01);

        public static float Pitch(float zoom01, GuildCameraSettings settings) =>
            Mathf.Lerp(settings.FarPitch, settings.NearPitch, zoom01);

        // The vertical FOV that keeps the reference pixel density for a texture of this height: a taller texture
        // sees more, never bigger. (Pixel world size stays 2·d·tan(refFov/2) ÷ refHeight.)
        public static float VerticalFov(float referenceFov, int renderHeight, int referenceHeight)
        {
            var halfTan = Mathf.Tan(referenceFov * 0.5f * Mathf.Deg2Rad) * renderHeight / Mathf.Max(1, referenceHeight);
            return 2f * Mathf.Atan(halfTan) * Mathf.Rad2Deg;
        }

        // Camera position and rotation looking at the focus point (snapped to the render-pixel grid if enabled).
        /// <param name="focus"></param>
        /// <param name="yaw"></param>
        /// <param name="zoom01"></param>
        /// <param name="settings"></param>
        /// <param name="referenceHeight">The reference render height (with the reference FOV it gives the true pixel size).</param>
        public static (Vector3 position, Quaternion rotation) Compute(Vector3 focus, float yaw, float zoom01,
                                                                      GuildCameraSettings settings, int referenceHeight)
        {
            var distance = Distance(zoom01, settings);
            var rotation = Quaternion.Euler(Pitch(zoom01, settings), yaw, 0f);
            var position = focus - rotation * Vector3.forward * distance;

            if (settings.PixelSnap)
                position = PixelSnap.Snap(position, rotation, PixelSnap.PixelWorldSize(distance, settings.FieldOfView, referenceHeight));

            return (position, rotation);
        }
    }
}