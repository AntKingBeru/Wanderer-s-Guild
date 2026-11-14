// The tutorial overlay's instruction box: the text, an optional Next button, and where it sits.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class TutorialBox
    {
        private readonly VisualElement _box;
        private readonly OutlinedText _text;
        private readonly OutlinedText _nextLabel;

        private TutorialBox(VisualElement box, OutlinedText text, Button next, OutlinedText nextLabel)
        {
            _box = box;
            _text = text;
            NextButton = next;
            _nextLabel = nextLabel;
        }

        public Button NextButton { get; }

        // Size after the last layout; zero before the first.
        public Vector2 Size
        {
            get
            {
                float width = _box.resolvedStyle.width, height = _box.resolvedStyle.height;
                return float.IsNaN(width) || float.IsNaN(height) ? Vector2.zero : new Vector2(width, height);
            }
        }

        // NOTE: Next gets the usual pixel-button feedback (pop + sound) like every panel button.
        public static TutorialBox Bind(VisualElement documentRoot, GameObject owner, Action onNext)
        {
            var box = documentRoot?.Q<VisualElement>("tutorial-box");
            var text = documentRoot?.Q<OutlinedText>("tutorial-text");
            var next = documentRoot?.Q<Button>("tutorial-next");
            var nextLabel = next?.Q<OutlinedText>("tutorial-next-label");
            if (box == null || text == null || next == null || nextLabel == null)
                return null;

            next.RemoveFromClassList(Button.ussClassName);
            next.AddManipulator(new MenuButtonFeedback(owner, pop: true));
            next.clicked += () => onNext?.Invoke();
            return new TutorialBox(box, text, next, nextLabel);
        }

        // NOTE: Text is only set when it changes (no relayout every frame).
        public void Show(string text, string nextLabel)
        {
            if (_text.Text != text)
                _text.Text = text;

            var hasNext = nextLabel != null;
            if (hasNext && _nextLabel.Text != nextLabel)
                _nextLabel.Text = nextLabel;

            NextButton.style.display = hasNext ? DisplayStyle.Flex : DisplayStyle.None;
            _box.style.display = DisplayStyle.Flex;
        }

        // NOTE: Invisible (but laid out) until it has a size to place it by.
        public void Place(Vector2 position, bool visible)
        {
            _box.style.left = position.x;
            _box.style.top = position.y;
            _box.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        public void Hide() => _box.style.display = DisplayStyle.None;

        public bool Contains(VisualElement element) => element != null && (element == _box || _box.Contains(element));
    }
}