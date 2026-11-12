// Projects a world point into the world view's local UI space, for elements placed over the world image.
using UnityEngine;

namespace WanderersGuild
{
    public static class WorldViewProjection
    {
        // NOTE: False behind the camera or outside the view. raiseUiPixels lifts the point by that many UI pixels
        // (converted to texture pixels with the render scale before mapping).
        public static bool TryToLocal(Camera camera, WorldViewPresenter view, Vector3 world, float raiseUiPixels, out Vector2 local)
        {
            local = default;
            if (camera == null || view == null)
                return false;

            var viewport = camera.WorldToViewportPoint(world);
            if (viewport.z <= 0f || viewport.x is < 0f or > 1f || viewport.y is < 0f or > 1f)
                return false;

            var size = view.RenderSize;
            Vector2 pixel = new(viewport.x * size.x, (1f - viewport.y) * size.y - raiseUiPixels * view.PixelScale);
            local = view.ViewToLocal(pixel);
            return true;
        }
    }
}