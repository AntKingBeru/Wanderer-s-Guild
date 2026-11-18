// A pixel-styled single-choice dropdown: the field shows Prefix + the chosen option; picking an item closes the list.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [UxmlElement]
    public sealed partial class PixelDropdown : PixelDropdownBase
    {
        private readonly List<string> _options = new();
        private Action<int> _onChanged;
        private string _prefix = string.Empty;

        // Text before the chosen option on the field ("Sort: " → "Sort: Rank"); the list shows the options alone.
        [UxmlAttribute]
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value ?? string.Empty;
                RefreshValue();
            }
        }

        public int Index { get; private set; }

        protected override int FocusIndex => Index;

        public void SetOptions(IReadOnlyList<string> options, int index, Action<int> onChanged)
        {
            Close(refocus: false);
            _options.Clear();
            _options.AddRange(options);
            _onChanged = onChanged;
            RebuildItems();
            SetIndexWithoutNotify(index);
        }

        public void SetIndexWithoutNotify(int index)
        {
            Index = _options.Count == 0 ? 0 : Mathf.Clamp(index, 0, _options.Count - 1);
            RefreshValue();
            Popup?.MarkCurrent(Index);
        }

        protected override void OnInitialized()
        {
            RebuildItems();
            Popup.MarkCurrent(Index);
        }

        private void RebuildItems()
        {
            if (Popup != null)
                Popup.SetItems(_options, Choose);
        }

        private void Choose(int index)
        {
            SetIndexWithoutNotify(index);
            Close();
            _onChanged?.Invoke(Index);
        }

        private void RefreshValue() => SetValueText(_options.Count == 0 ? string.Empty : _prefix + _options[Index]);
    }
}