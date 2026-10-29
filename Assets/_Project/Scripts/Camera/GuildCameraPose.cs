// The guild camera's pose maths (shared by the rig at runtime and the editor preview): orbit position, tilt, pixel snap.
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

        // Camera position and rotation looking at the focus point (snapped to the render-pixel grid if enabled).
        public static (Vector3 position, Quaternion rotation) Compute(Vector3 focus, float yaw, float zoom01,
            GuildCameraSettings settings, int renderHeight)
        {
            var distance = Distance(zoom01, settings);
            var rotation = Quaternion.Euler(Pitch(zoom01, settings), yaw, 0f);
            var position = focus - rotation * Vector3.forward * distance;

            if (settings.PixelSnap)
                position = PixelSnap.Snap(position, rotation, PixelSnap.PixelWorldSize(distance, settings.FieldOfView, renderHeight));

            return (position, rotation);
        }
    }
}