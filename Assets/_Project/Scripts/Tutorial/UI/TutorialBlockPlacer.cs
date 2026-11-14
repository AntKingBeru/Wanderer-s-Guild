// Where the instruction box goes: under what it points at, else above, right or left — or under the HUD bar when it points at nothing.
using UnityEngine;

namespace WanderersGuild
{
    public static class TutorialBoxPlacer
    {
        // NOTE: Overlay-local space (UI pixels). The first side with room wins; with none, the box keeps to the bottom of the
        // screen (it may cover the target, but never leaves the screen). Whole pixels.
        public static Vector2 Place(Vector2 box, Vector2 screen, bool hasTarget, Rect target, TutorialSettings settings)
        {
            float gap = settings.BoxGap, margin = settings.ScreenMargin;
            if (!hasTarget)
                return Clamp(new Vector2((screen.x - box.x) * 0.5f, settings.FreeBoxTop), box, screen, margin);

            var centredX = target.center.x - box.x * 0.5f;
            var centredY = target.center.y - box.y * 0.5f;

            if (target.yMax + gap + box.y <= screen.y - margin)
                return Clamp(new Vector2(centredX, target.yMax + gap), box, screen, margin);
            if (target.yMin - gap - box.y >= margin)
                return Clamp(new Vector2(centredX, target.yMin - gap - box.y), box, screen, margin);
            if (target.xMax + gap + box.x <= screen.x - margin)
                return Clamp(new Vector2(target.xMax + gap, centredY), box, screen, margin);
            if (target.xMin - gap - box.x >= margin)
                return Clamp(new Vector2(target.xMin - gap - box.x, centredY), box, screen, margin);

            return Clamp(new Vector2(centredX, screen.y - margin - box.y), box, screen, margin);
        }

        private static Vector2 Clamp(Vector2 position, Vector2 box, Vector2 screen, float margin)
        {
            var x = Mathf.Clamp(position.x, margin, Mathf.Max(margin, screen.x - margin - box.x));
            var y = Mathf.Clamp(position.y, margin, Mathf.Max(margin, screen.y - margin - box.y));
            return new Vector2(Mathf.Round(x), Mathf.Round(y));
        }
    }
}