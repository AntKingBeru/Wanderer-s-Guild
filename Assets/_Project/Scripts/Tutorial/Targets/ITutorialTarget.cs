// Something a tutorial step points at — a UI element, a world object or several at once (Strategy).
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public interface ITutorialTarget
    {
        // False while it can't be shown (its screen closed, hidden, gone).
        bool IsAvailable { get; }

        // The UI element (blocker holes, clicks, keyboard focus); null for world objects.
        VisualElement Element { get; }

        // Where it is on screen, in panel space. False when it can't be placed this frame.
        bool TryGetRect(out Rect panelRect);

        // The step started (true) or ended (false): scroll it into view, outline it…
        void SetEmphasis(bool on);
    }
}