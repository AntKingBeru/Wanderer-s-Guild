// The tutorial overlay's spotlight: four dim panels around the target and a blinking frame on it.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class TutorialSpotlight
    {
        private const string BlinkOffClass = "tutorial-frame--off";

        private readonly VisualElement _top;
        private readonly VisualElement _bottom;
        private readonly VisualElement _left;
        private readonly VisualElement _right;
        private readonly VisualElement _frame;

        private TutorialSpotlight(VisualElement top, VisualElement bottom, VisualElement left, VisualElement right, VisualElement frame)
        {
            _top = top;
            _bottom = bottom;
            _left = left;
            _right = right;
            _frame = frame;
        }

        public static TutorialSpotlight Bind(VisualElement documentRoot)
        {
            var top = documentRoot?.Q<VisualElement>("tutorial-dim-top");
            var bottom = documentRoot?.Q<VisualElement>("tutorial-dim-bottom");
            var left = documentRoot?.Q<VisualElement>("tutorial-dim-left");
            var right = documentRoot?.Q<VisualElement>("tutorial-dim-right");
            var frame = documentRoot?.Q<VisualElement>("tutorial-frame");

            return top != null && bottom != null && left != null && right != null && frame != null
                ? new TutorialSpotlight(top, bottom, left, right, frame)
                : null;
        }

        // The frame's rect: the target grown by the padding, in whole pixels (the box keeps clear of this too).
        public static Rect Framed(Rect target, int padding) =>
            Rect.MinMaxRect(Mathf.Floor(target.xMin) - padding, Mathf.Floor(target.yMin) - padding,
                            Mathf.Ceil(target.xMax) + padding, Mathf.Ceil(target.yMax) + padding);

        // NOTE: Overlay-local space. The dims cover the rest of the screen (clamped to it), so a target partly off-screen still works.
        public void Show(Rect target, Vector2 screen, int padding, bool blinkOn)
        {
            var frame = Framed(target, padding);
            float x0 = Mathf.Clamp(frame.xMin, 0f, screen.x), x1 = Mathf.Clamp(frame.xMax, 0f, screen.x);
            float y0 = Mathf.Clamp(frame.yMin, 0f, screen.y), y1 = Mathf.Clamp(frame.yMax, 0f, screen.y);

            Place(_top, 0f, 0f, screen.x, y0);
            Place(_bottom, 0f, y1, screen.x, screen.y - y1);
            Place(_left, 0f, y0, x0, y1 - y0);
            Place(_right, x1, y0, screen.x - x1, y1 - y0);
            Place(_frame, frame.x, frame.y, frame.width, frame.height);
            _frame.EnableInClassList(BlinkOffClass, !blinkOn);
        }

        public void Hide()
        {
            _top.style.display = DisplayStyle.None;
            _bottom.style.display = DisplayStyle.None;
            _left.style.display = DisplayStyle.None;
            _right.style.display = DisplayStyle.None;
            _frame.style.display = DisplayStyle.None;
        }

        private static void Place(VisualElement element, float x, float y, float width, float height)
        {
            element.style.left = x;
            element.style.top = y;
            element.style.width = Mathf.Max(0f, width);
            element.style.height = Mathf.Max(0f, height);
            element.style.display = DisplayStyle.Flex;
        }
    }
}