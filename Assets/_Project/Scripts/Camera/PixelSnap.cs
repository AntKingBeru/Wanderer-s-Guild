// Pixel-snapping maths for a perspective camera: world size of one render pixel at a distance, and grid-snapped positions.
using UnityEngine;

namespace WanderersGuild
{
    public static class PixelSnap
    {
        // World units covered by one render pixel at this distance (vertical FOV, render height in pixels).
        public static float PixelWorldSize(float distance, float verticalFov, int renderHeight) =>
            2f * distance * Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) / Mathf.Max(1, renderHeight);

        // Rounds the position to the pixel grid on the camera's own right/up axes (forward is left alone).
        public static Vector3 Snap(Vector3 position, Quaternion rotation, float pixelSize)
        {
            if (pixelSize <= 0f)
                return position;

            var local = Quaternion.Inverse(rotation) * position;
            local.x = Mathf.Round(local.x / pixelSize) * pixelSize;
            local.y = Mathf.Round(local.y / pixelSize) * pixelSize;
            return rotation * local;
        }
    }
}