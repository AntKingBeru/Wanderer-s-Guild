// Pixel-art text with a crisp, square 1-pixel outline: eight offset outline copies behind one fill label, inside a 2px
// left/right gutter, optically centred (the font's glyphs sit low and left in their line box).
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [UxmlElement]
    public partial class OutlinedText : VisualElement
    {
        public const string UssClassName = "outlined-text";
        public const string StackUssClassName = UssClassName + "__stack";
        public const string PartUssClassName = UssClassName + "__part";
        public const string OutlineUssClassName = UssClassName + "__outline";
        public const string FillUssClassName = UssClassName + "__fill";

        // NOTE: The 8 neighbors of a pixel; each outline copy is shifted by exactly one UI pixel.
        private static readonly Vector2[] OutlineOffsets =
        {
            new(-1f, -1f), new(0f, -1f), new(1f, -1f), new(-1f, 0f),
            new(1f, 0f), new(-1f, 1f), new(0f, 1f), new(1f, 1f)
        };

        // NOTE: Optical centring (PixelTheme.uss › .outlined-text). monogram at 12px draws capitals 0.62px and lowercase 1.37px
        // below the line box's center, and every glyph 0.375px left of its advance — so text is shifted by (x, y) to look
        // centred. A "translate": layout, sizes and measurements never change.
        private static readonly CustomStyleProperty<float> OpticalXProperty = new("--text-optical-x");
        private static readonly CustomStyleProperty<float> OpticalYProperty = new("--text-optical-y");

        private readonly Label[] _outlines = new Label[8];
        private readonly Label _fill;
        private string _text = string.Empty;
        private Vector2 _optical;

        // The displayed text. Set in UXML with the "text" attribute.
        [UxmlAttribute]
        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? string.Empty;
                foreach (var outline in _outlines)
                    outline.text = _text;
                _fill.text = _text;
            }
        }

        public OutlinedText()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;

            // NOTE: All copies live in one wrapper that hugs the fill. Alignment/justification/padding applied to this
            // element moves the wrapper as a whole, so the outline can never drift away from the fill.
            VisualElement stack = new() { pickingMode = PickingMode.Ignore };
            stack.AddToClassList(StackUssClassName);
            Add(stack);

            // NOTE: Child order is draw order — outlines first (behind), fill last (on top). Every part carries the same
            // gutter (PixelTheme.uss › .outlined-text__part), so the copies cover exactly the fill's box.
            for (var i = 0; i < OutlineOffsets.Length; i++)
            {
                var outline = CreatePart(OutlineUssClassName);
                outline.style.position = Position.Absolute;
                outline.style.left = 0f;
                outline.style.top = 0f;
                outline.style.right = 0f;
                outline.style.bottom = 0f;

                _outlines[i] = outline;
                stack.Add(outline);
            }

            // The fill stays in normal flow, so it alone decides the wrapper's (and this element's) size.
            _fill = CreatePart(FillUssClassName);
            stack.Add(_fill);

            ApplyOffsets();
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        // NOTE: The left + right gutter every part carries (0 until styles resolve — callers measure after layout).
        private float HorizontalGutter
        {
            get
            {
                var newStyle = _fill.resolvedStyle;
                var gutter = newStyle.paddingLeft + newStyle.paddingRight;
                return float.IsNaN(gutter) ? 0f : gutter;
            }
        }

        // Overrides the fill (text) color. Pass StyleKeyword.Null to return to the stylesheet color.
        public void SetFillColor(StyleColor color) => _fill.style.color = color;

        // Height the given text would take in an element this wide, wrapped and styled exactly like this element's text.
        // NOTE: The glyphs wrap inside the gutter, so the wrap width is the element's width minus it.
        public float MeasureTextHeight(string text, float width)
        {
            var inner = Mathf.Max(0f, width - HorizontalGutter);
            return _fill.MeasureTextSize(text ?? string.Empty, inner, MeasureMode.Exactly, 0f, MeasureMode.Undefined).y;
        }

        // Width this element's current text needs on one line per explicit line break (unwrapped), styled exactly like it
        // and including the gutter — what a sliding (marquee) line compares with its box, so the slide ends with the
        // last glyph's outline in view.
        public float MeasureTextWidth() =>
            _fill.MeasureTextSize(_text, 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined).x + HorizontalGutter;

        // Sets different strings for the fill and the 8 outline copies (e.g. a typewriter: the fill keeps the hidden
        // tail with a transparent tag, the outlines get it blanked, because the SDF outline ignores rich-text alpha).
        // NOTE: Both strings must lay out identically (same length per line, same break points), or the outline drifts.
        public void SetTexts(string fill, string outline)
        {
            _text = fill ?? string.Empty;
            var outlineText = outline ?? _text;
            foreach (var copy in _outlines)
                copy.text = outlineText;
            _fill.text = _text;
        }

        // NOTE: Only re-applies when the resolved values actually change (style resolution, never per frame).
        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var x = evt.customStyle.TryGetValue(OpticalXProperty, out var opticalX) ? opticalX : 0f;
            var y = evt.customStyle.TryGetValue(OpticalYProperty, out var opticalY) ? opticalY : 0f;
            var optical = new Vector2(x, y);
            if (optical == _optical)
                return;

            _optical = optical;
            ApplyOffsets();
        }

        // NOTE: Inline translates — the outline copies already need one (their ±1px offset), so the optical offset is added in
        // code; the fill gets the same offset, keeping the outline exactly around it.
        private void ApplyOffsets()
        {
            for (var i = 0; i < _outlines.Length; i++)
                _outlines[i].style.translate = new Translate(OutlineOffsets[i].x + _optical.x, OutlineOffsets[i].y + _optical.y);
            _fill.style.translate = new Translate(_optical.x, _optical.y);
        }

        private static Label CreatePart(string partClass)
        {
            Label label = new();
            label.RemoveFromClassList(Label.ussClassName); // NOTE: Drops default label padding/margins so all copies align exactly.
            label.AddToClassList(PartUssClassName);
            label.AddToClassList(partClass);
            label.pickingMode = PickingMode.Ignore;
            return label;
        }
    }
}