// The tutorial overlay's instruction box: the text, an optional controls card (control → keys), an optional Next button.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class TutorialBox
    {
        private const string RowClass = "tutorial-controls__row";

        private readonly VisualElement _box;
        private readonly OutlinedText _text;
        private readonly OutlinedText _nextLabel;
        private readonly VisualElement _controls;
        private readonly List<VisualElement> _rows = new();
        private IReadOnlyList<ControlEntry> _shownControls;

        private TutorialBox(VisualElement box, OutlinedText text, VisualElement controls, Button next, OutlinedText nextLabel)
        {
            _box = box;
            _text = text;
            _controls = controls;
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

        // NOTE: Next gets the usual pixel-button feedback (pop + sound) like every panel button. The controls card is optional
        // (an older overlay without it just shows no rows).
        public static TutorialBox Bind(VisualElement documentRoot, GameObject owner, Action onNext)
        {
            var box = documentRoot?.Q<VisualElement>("tutorial-box");
            var text = documentRoot?.Q<OutlinedText>("tutorial-text");
            var controls = documentRoot?.Q<VisualElement>("tutorial-controls");
            var next = documentRoot?.Q<Button>("tutorial-next");
            var nextLabel = next?.Q<OutlinedText>("tutorial-next-label");
            if (box == null || text == null || next == null || nextLabel == null)
                return null;

            if (controls == null)
                Debug.LogWarning("[TutorialBox] No 'tutorial-controls' in TutorialOverlay.uxml — controls cards won't show.");

            next.RemoveFromClassList(Button.ussClassName);
            next.AddManipulator(new MenuButtonFeedback(owner, pop: true));
            next.clicked += () => onNext?.Invoke();
            return new TutorialBox(box, text, controls, next, nextLabel);
        }

        // NOTE: Text is only set when it changes, and the card only rebuilt when its list changes (no relayout every frame).
        public void Show(string text, string nextLabel, IReadOnlyList<ControlEntry> controls)
        {
            if (_text.Text != text)
                _text.Text = text;
            if (!ReferenceEquals(controls, _shownControls))
                ShowControls(controls);

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

        // NOTE: Rows are pooled (created on first need, hidden when unused); the keys are read now, so a rebinding shows.
        private void ShowControls(IReadOnlyList<ControlEntry> controls)
        {
            _shownControls = controls;
            if (_controls == null)
                return;

            var count = controls?.Count ?? 0;
            while (_rows.Count < count)
                _rows.Add(CreateRow());

            for (var i = 0; i < _rows.Count; i++)
            {
                var visible = i < count;
                _rows[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible)
                    continue;

                if (controls != null)
                {
                    ((OutlinedText)_rows[i][0]).Text = controls[i].Label;
                    ((OutlinedText)_rows[i][1]).Text = controls[i].KeysText();
                }
            }

            _controls.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // NOTE: Child 0 = the control's name, child 1 = its keys (read by index above).
        private VisualElement CreateRow()
        {
            VisualElement row = new() { pickingMode = PickingMode.Ignore };
            row.AddToClassList(RowClass);

            OutlinedText label = new();
            label.AddToClassList(RowClass + "-label");
            OutlinedText keys = new();
            keys.AddToClassList(RowClass + "-keys");

            row.Add(label);
            row.Add(keys);
            _controls.Add(row);
            return row;
        }
    }
}