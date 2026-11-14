// A tutorial target in the UI, found by its query every time it's used (screens re-fill pooled rows); brought into view.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class TutorialUiTarget : ITutorialTarget
    {
        private readonly Func<VisualElement> _find;
        private readonly Action _reveal;

        // NOTE: reveal (optional) brings it into view where a plain scroll can't (the build catalog's strip).
        public TutorialUiTarget(Func<VisualElement> find, Action reveal = null)
        {
            _find = find;
            _reveal = reveal;
        }

        // The element while it's showing (null otherwise).
        public VisualElement Element
        {
            get
            {
                var element = _find?.Invoke();
                return IsShown(element) ? element : null;
            }
        }

        public bool IsAvailable => TryGetRect(out _);

        // NOTE: NaN (before layout) fails the size checks.
        public bool TryGetRect(out Rect panelRect)
        {
            var element = Element;
            panelRect = element?.worldBound ?? default;
            return element != null && panelRect is { width: > 0f, height: > 0f };
        }

        // NOTE: As the step starts: the reveal action, then — inside a scroll view (roster details, applicants) — a scroll to it.
        public void SetEmphasis(bool on)
        {
            if (!on)
                return;

            _reveal?.Invoke();
            var element = Element;
            var scroll = element?.GetFirstAncestorOfType<ScrollView>();
            scroll?.ScrollTo(element);
        }

        // NOTE: Hidden if it or any ancestor isn't displayed or visible (resolved style — a frame behind a change, which the
        // director's polling absorbs).
        public static bool IsShown(VisualElement element)
        {
            if (element?.panel == null)
                return false;

            for (var current = element; current != null; current = current.parent)
            {
                var style = current.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden)
                    return false;
            }
            return true;
        }
    }
}