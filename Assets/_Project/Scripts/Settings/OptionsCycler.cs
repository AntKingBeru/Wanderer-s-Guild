using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [UxmlElement]
    public sealed partial class OptionCycler : VisualElement
    {
        public const string UssClassName = "option-cycler";
        public const string ArrowUssClassName = UssClassName + "__arrow";
        public const string ValueUssClassName = UssClassName + "__value";

        private readonly OutlinedText _value;
        private readonly List<string> _options = new();
        private Action<int> _onChanged;

        public int Index { get; private set; }

        public OptionCycler()
        {
            AddToClassList(UssClassName);
            focusable = true;

            _value = new OutlinedText();
            _value.AddToClassList(ValueUssClassName);

            Add(CreateArrow("<", -1));
            Add(_value);
            Add(CreateArrow(">", 1));

            RegisterCallback<NavigationMoveEvent>(OnMove);
            RegisterCallback<PointerEnterEvent>(_ => { if (enabledInHierarchy && focusable) Focus(); }); // Hover selects, like buttons.
            RegisterCallback<FocusEvent>(_ => GameEventsRelay.OnUIButtonFocused.Invoke());
        }

        // Replaces the values. The callback receives the new index after every player change.
        public void SetOptions(IReadOnlyList<string> options, int index, Action<int> onChanged)
        {
            _options.Clear();
            _options.AddRange(options);
            _onChanged = onChanged;
            SetIndexWithoutNotify(index);
        }

        public void SetIndexWithoutNotify(int index)
        {
            Index = _options.Count == 0 ? 0 : Mathf.Clamp(index, 0, _options.Count - 1);
            _value.Text = _options.Count == 0 ? string.Empty : _options[Index];
        }

        private Button CreateArrow(string glyph, int step)
        {
            Button arrow = new(() => Step(step)) { focusable = false };
            arrow.RemoveFromClassList(Button.ussClassName);
            arrow.AddToClassList(ArrowUssClassName);
            arrow.Add(new OutlinedText { Text = glyph });
            return arrow;
        }

        // NOTE: Left/Right bubble up from this focused row and are consumed, so the panel's focus ring stays put.
        private void OnMove(NavigationMoveEvent evt)
        {
            var step = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => -1,
                NavigationMoveEvent.Direction.Right => 1,
                _ => 0
            };
            if (step == 0)
                return;

            Step(step);
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }

        private void Step(int step)
        {
            if (_options.Count == 0 || !enabledInHierarchy)
                return;

            if (focusController?.focusedElement != this)
                Focus(); // Clicking an arrow also selects the row.

            Index = (Index + step + _options.Count) % _options.Count;
            _value.Text = _options[Index];
            GameEventsRelay.OnUIButtonClicked.Invoke();
            _onChanged?.Invoke(Index);
        }
    }
}