// The tutorial overlay's click blocker: stops clicks on the UI except inside its holes; the world under it stays clickable.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Picking asks ContainsPoint. In a hole → false (the click reaches the screen under it).Elsewhere, it probes what's
    // under it with itself switched off: some UI → true (blocked); nothing → false, so the world (camera, the step's world
    // target) still gets the pointer — world clicks are gated by TutorialGate instead.
    [UxmlElement]
    public partial class TutorialBlocker : VisualElement
    {
        private readonly List<VisualElement> _holes = new();
        private bool _isProbing;

        public TutorialBlocker() => pickingMode = PickingMode.Position;

        public void SetHoles(IReadOnlyList<VisualElement> holes)
        {
            _holes.Clear();
            if (holes == null)
                return;

            foreach (var hole in holes)
            {
                if (hole != null)
                    _holes.Add(hole);
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (_isProbing || !base.ContainsPoint(localPoint))
                return false;

            var point = this.LocalToWorld(localPoint);
            return !_holes.Any(hole => hole.panel != null && hole.worldBound.Contains(point)) && IsUiUnder(point);
        }

        // NOTE: The probe re-enters ContainsPoint on this element — the flag makes it see through itself.
        private bool IsUiUnder(Vector2 point)
        {
            if (panel == null)
                return true;

            _isProbing = true;
            try
            {
                return panel.Pick(point) != null;
            }
            finally
            {
                _isProbing = false;
            }
        }
    }
}