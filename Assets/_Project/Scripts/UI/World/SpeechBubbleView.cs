// One speech bubble over the world view: a parchment box with a pixel tail whose tip sits on the anchor; fades in and out.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class SpeechBubbleView
    {
        private const string RootClass = "speech-bubble";
        private const string BoxClass = "speech-bubble__box";
        private const string TextClass = "speech-bubble__text";
        private const string TailClass = "speech-bubble__tail";
        private const string RowClass = "speech-bubble__tail-row";

        private readonly VisualElement _root;
        private readonly Label _text;

        public SpeechBubbleView(VisualElement layer)
        {
            _root = Part(RootClass);

            var box = Part(BoxClass);
            _text = new Label { pickingMode = PickingMode.Ignore };
            _text.AddToClassList(TextClass);
            box.Add(_text);
            _root.Add(box);

            // NOTE: Child order = draw order — the tail comes after the box, so its first row opens the box's bottom border.
            var tail = Part(TailClass);
            tail.Add(Part(RowClass, RowClass + "--wide"));
            tail.Add(Part(RowClass, RowClass + "--narrow"));
            tail.Add(Part(RowClass, RowClass + "--tip"));
            _root.Add(tail);

            layer.Add(_root);
            Hide();
        }

        // Starts a line: laid out, but invisible until placed and faded in.
        public void Show(string line)
        {
            _text.text = line;
            _root.style.opacity = 0f;
            _root.style.visibility = Visibility.Hidden;
            _root.style.display = DisplayStyle.Flex;
        }

        // NOTE: anchor = the tail's tip in the layer's local space; whole pixels keep the pixel font crisp.
        public void Place(Vector2 anchor)
        {
            _root.style.left = Mathf.Round(anchor.x);
            _root.style.top = Mathf.Round(anchor.y);
            _root.style.visibility = Visibility.Visible;
        }

        // Hides without ending (hovered speaker, head off-screen) — layout is kept, so it reappears in place.
        public void SetVisible(bool visible) => _root.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;

        public void SetOpacity(float opacity) => _root.style.opacity = Mathf.Clamp01(opacity);

        public void Hide() => _root.style.display = DisplayStyle.None;

        // NOTE: Every part ignores picking — a bubble must never block clicks on the world under it.
        private static VisualElement Part(params string[] classes)
        {
            VisualElement element = new() { pickingMode = PickingMode.Ignore };
            foreach (var name in classes)
                element.AddToClassList(name);
            return element;
        }
    }
}