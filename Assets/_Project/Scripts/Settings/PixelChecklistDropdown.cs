// A pixel-styled multiple-choice dropdown: every item has a checkbox and toggles without closing the list; the field shows
// a summary its owner sets.
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [UxmlElement]
    public sealed partial class PixelChecklistDropdown : PixelDropdownBase
    {
        public const string CheckUssClassName = "pixel-dropdown__check";
        public const string CheckedUssClassName = CheckUssClassName + "--on";

        private readonly List<string> _options = new();
        private readonly List<VisualElement> _checks = new();
        private Func<int, bool> _isChecked;
        private Action<int> _onToggled;

        protected override int FocusIndex => 0;

        // NOTE: isChecked(i) is read back after every toggle, so the owner decides what a toggle means ("All" clears the rest).
        public void SetOptions(IReadOnlyList<string> options, Func<int, bool> isChecked, Action<int> onToggled)
        {
            Close(refocus: false);
            _options.Clear();
            _options.AddRange(options);
            _isChecked = isChecked;
            _onToggled = onToggled;
            RebuildItems();
        }

        public void SetLabel(string text) => SetValueText(text);

        public void RefreshChecks()
        {
            for (var i = 0; i < _checks.Count; i++)
                _checks[i].EnableInClassList(CheckedUssClassName, _isChecked != null && _isChecked(i));
        }

        protected override void OnInitialized() => RebuildItems();

        private void RebuildItems()
        {
            if (Popup == null)
                return;

            _checks.Clear();
            Popup.SetItems(_options, Toggle, CreateCheck);
            RefreshChecks();
        }

        private VisualElement CreateCheck(int _)
        {
            VisualElement check = new() { pickingMode = PickingMode.Ignore };
            check.AddToClassList(CheckUssClassName);
            _checks.Add(check);
            return check;
        }

        // NOTE: The list stays open (several choices); focus stays on the toggled item.
        private void Toggle(int index)
        {
            _onToggled?.Invoke(index);
            RefreshChecks();
        }
    }
}