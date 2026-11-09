// Narration box that grows with the typed text: shows only revealed lines, follows the newest line, mouse wheel scrolls back.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ScrollingTextBox : IRevealTarget
    {
        // NOTE: Everything after this tag is drawn fully transparent but still laid out, so words never jump lines.
        private const string HiddenTag = "<alpha=#00>";

        // NOTE: The clip's 1px padding on each side (top + bottom), which keeps the 1px outline from being cut off.
        private const float OutlinePadding = 2f;

        private readonly ScrollView _scroll;
        private readonly VisualElement _clip;
        private readonly OutlinedText _text;
        private float _clipHeight = -1f;
        private bool _followPending;

        public ScrollingTextBox(ScrollView scroll, VisualElement clip, OutlinedText text, float wheelStep)
        {
            _scroll = scroll;
            _clip = clip;
            _text = text;
            _scroll.mouseWheelScrollSize = wheelStep;

            // NOTE: Always laid out, so the text width never changes when it's needed (that would re-wrap mid-typing).
            // It's made visible only while the text overflows, and it's never clickable — a click also counts as
            // "any key" and would advance the slide.
            _scroll.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _scroll.verticalScroller.pickingMode = PickingMode.Ignore;
            _scroll.verticalScroller.Query<VisualElement>().ForEach(part => part.pickingMode = PickingMode.Ignore);

            _scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(OnContentResized);
            UpdateScrollerVisibility();
        }

        // NOTE: The fill keeps the untyped tail (transparent) so wrapping never changes; the outline copies get that tail
        // blanked with non-breaking spaces — the SDF outline ignores <alpha>, so a transparent tail still drew its outline.
        // monogram is monospaced, so an NBSP is exactly one glyph wide and spaces/line breaks keep the same word breaks.
        public void Show(string full, int shown)
        {
            full ??= string.Empty;
            var count = Mathf.Clamp(shown, 0, full.Length);

            if (count >= full.Length) _text.Text = full;
            else
            {
                string visible = full[..count], tail = full[count..];
                _text.SetTexts(visible + HiddenTag + tail, visible + Blank(tail));
            }

            if (count == 0)
            {
                SetClipHeight(0f);
                return;
            }

            var width = _text.contentRect.width;
            if (float.IsNaN(width) || width <= 0f)
            {
                // NOTE: Not laid out yet — show it unclipped once instead of measuring against a width of 0.
                _clip.style.height = StyleKeyword.Auto;
                _clipHeight = -1f;
                return;
            }

            var height = Mathf.Ceil(_text.MeasureTextHeight(full[..WordEnd(full, count)], width));
            SetClipHeight(height);
        }
        
        // NOTE: Every character except spaces and line breaks becomes a non-breaking space (same width, draws nothing,
        // never a break point — so the words wrap exactly where the fill's do).
        private static string Blank(string tail)
        {
            var chars = tail.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != ' ' && chars[i] != '\n')
                    chars[i] = '\u00A0';
            }
            return new string(chars);
        }

        public void Clear()
        {
            _text.Text = string.Empty;
            _followPending = false;
            SetClipHeight(0f);
            _scroll.scrollOffset = Vector2.zero;
        }

        // NOTE: The clip is sized to the revealed lines only, so the scroll range never reaches the invisible tail.
        private void SetClipHeight(float textHeight)
        {
            var height = textHeight > 0f ? textHeight + OutlinePadding : 0f;
            if (Mathf.Approximately(height, _clipHeight))
                return;

            if (height > _clipHeight)
                _followPending = true; // A new line appeared — follow it once layout updates.
            _clipHeight = height;
            _clip.style.height = height;
        }

        // NOTE: Runs after layout, when the new content height is known.
        private void OnContentResized(GeometryChangedEvent _)
        {
            UpdateScrollerVisibility();
            if (!_followPending)
                return;

            _followPending = false;
            var maxOffset = Mathf.Max(0f, _scroll.contentContainer.layout.height - _scroll.contentViewport.layout.height);
            _scroll.scrollOffset = new Vector2(0f, maxOffset);
        }

        private void UpdateScrollerVisibility()
        {
            var overflows = _scroll.contentContainer.layout.height > _scroll.contentViewport.layout.height + 0.5f;
            _scroll.verticalScroller.style.opacity = overflows ? 1f : 0f;
        }

        // NOTE: Measures to the end of the word being typed — a half-typed word may fit a line the whole word won't.
        private static int WordEnd(string full, int count)
        {
            if (char.IsWhiteSpace(full[count - 1]))
                return count;

            var end = count;
            while (end < full.Length && !char.IsWhiteSpace(full[end]))
                end++;
            return end;
        }
    }
}