// Several tutorial targets framed as one (a title and the line under it); the first one's element is used for clicks and holes.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class TutorialUnionTarget : ITutorialTarget
    {
        private readonly ITutorialTarget[] _parts;

        public TutorialUnionTarget(params ITutorialTarget[] parts) => _parts = parts ?? Array.Empty<ITutorialTarget>();

        public bool IsAvailable => TryGetRect(out _);

        public VisualElement Element => _parts.Length > 0 ? _parts[0].Element : null;

        // NOTE: Parts that can't be placed are left out; true while at least one can.
        public bool TryGetRect(out Rect panelRect)
        {
            panelRect = default;
            var any = false;
            foreach (var part in _parts)
            {
                if (part == null || !part.TryGetRect(out var rect))
                    continue;

                panelRect = any ? Rect.MinMaxRect(Mathf.Min(panelRect.xMin, rect.xMin), Mathf.Min(panelRect.yMin, rect.yMin),
                        Mathf.Max(panelRect.xMax, rect.xMax), Mathf.Max(panelRect.yMax, rect.yMax))
                    : rect;
                any = true;
            }
            return any;
        }

        public void SetEmphasis(bool on)
        {
            foreach (var part in _parts)
                part?.SetEmphasis(on);
        }
    }
}