// Projects world bounds onto the screen as a panel-space rectangle (to frame world objects in the tutorial overlay).
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: The world is drawn into a texture shown by WorldViewPresenter — corners go world → viewport → view pixel → panel.
    public sealed class TutorialWorldProjector
    {
        private readonly Camera _camera;
        private readonly WorldViewPresenter _view;

        public TutorialWorldProjector(Camera camera, WorldViewPresenter view)
        {
            _camera = camera;
            _view = view;
        }

        // NOTE: All 8 corners must be in front of the camera; the rect is their screen extent (it may reach off-screen).
        public bool TryProject(Bounds bounds, out Rect panelRect)
        {
            panelRect = default;
            if (_camera == null || _view == null)
                return false;

            var size = _view.RenderSize;
            if (size.x <= 0 || size.y <= 0)
                return false;

            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);
            for (var i = 0; i < 8; i++)
            {
                Vector3 corner = new((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                var viewport = _camera.WorldToViewportPoint(corner);
                if (viewport.z <= 0f)
                    return false;

                var point = _view.ViewToPanel(new Vector2(viewport.x * size.x, (1f - viewport.y) * size.y));
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            panelRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }
    }
}