// Pixel-art text with a crisp, square 1-pixel outline: eight offset outline copies drawn behind one fill label.
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

        private readonly Label[] _outlines = new Label[8];
        private readonly Label _fill;
        private string _text = string.Empty;

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

            // NOTE: Child order is draw order — outlines first (behind), fill last (on top).
            for (var i = 0; i < OutlineOffsets.Length; i++)
            {
                var outline = CreatePart(OutlineUssClassName);
                outline.style.position = Position.Absolute;
                outline.style.left = 0f;
                outline.style.top = 0f;
                outline.style.right = 0f;
                outline.style.bottom = 0f;
                outline.style.translate = new Translate(OutlineOffsets[i].x, OutlineOffsets[i].y);

                _outlines[i] = outline;
                stack.Add(outline);
            }

            // The fill stays in normal flow, so it alone decides the wrapper's (and this element's) size.
            _fill = CreatePart(FillUssClassName);
            stack.Add(_fill);
        }

        // Overrides the fill (text) color. Pass StyleKeyword.Null to return to the stylesheet color.
        public void SetFillColor(StyleColor color) => _fill.style.color = color;

        // Height the given text would take at this width, wrapped and styled exactly like this element's text.
        public float MeasureTextHeight(string text, float width) =>
            _fill.MeasureTextSize(text ?? string.Empty, width, MeasureMode.Exactly, 0f, MeasureMode.Undefined).y;

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