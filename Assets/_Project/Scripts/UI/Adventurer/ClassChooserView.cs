// The roster's class chooser: an overlay with one button per offered class (pooled) and Cancel.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ClassChooserView
    {
        private const int MaxOptions = 4;

        private readonly VisualElement _overlay;
        private readonly List<Button> _options = new();
        private readonly List<OutlinedText> _labels = new();
        private readonly List<Button> _buttons = new();
        private readonly List<ClassData> _shown = new();
        private Action<ClassData> _onPick;
        private bool _warned;

        public Button Cancel { get; }
        public bool IsOpen { get; private set; }

        // The adventurer the chooser was opened for (−1 when closed).
        public int AdventurerId { get; private set; } = -1;

        // Option buttons + Cancel, for MenuPanel setup and navigation.
        public IReadOnlyList<Button> Buttons => _buttons;

        private ClassChooserView(VisualElement overlay, VisualElement list, Button cancel)
        {
            _overlay = overlay;
            Cancel = cancel;

            for (var i = 0; i < MaxOptions; i++)
            {
                var index = i;
                Button button = new(() => Pick(index));
                button.AddToClassList("pixel-button");
                button.AddToClassList("chooser-option");
                OutlinedText label = new() { pickingMode = PickingMode.Ignore };
                button.Add(label);
                list.Add(button);

                _options.Add(button);
                _labels.Add(label);
                _buttons.Add(button);
            }
            _buttons.Add(cancel);
            HideImmediate();
        }

        public static ClassChooserView Bind(VisualElement root)
        {
            var overlay = root.Q<VisualElement>("chooser-overlay");
            var list = root.Q<VisualElement>("chooser-options");
            var cancel = root.Q<Button>("chooser-cancel");
            return overlay != null && list != null && cancel != null ? new ClassChooserView(overlay, list, cancel) : null;
        }

        // Shows the offered classes (up to 4; extras warned once) for this adventurer.
        public void Show(int adventurerId, IReadOnlyList<ClassData> options, Action<ClassData> onPick)
        {
            if (options.Count > MaxOptions && !_warned)
            {
                _warned = true;
                Debug.LogWarning($"[ClassChooserView] {options.Count} upgrade options but only {MaxOptions} buttons; extras aren't shown.");
            }

            _shown.Clear();
            for (var i = 0; i < _options.Count; i++)
            {
                var visible = i < options.Count;
                _options[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                _options[i].SetEnabled(visible);
                if (!visible)
                    continue;

                _shown.Add(options[i]);
                _labels[i].Text = AdvancementFormatter.Option(options[i]);
            }

            _onPick = onPick;
            AdventurerId = adventurerId;
            IsOpen = true;
            _overlay.style.display = DisplayStyle.Flex;

            var first = _options.Count > 0 && _shown.Count > 0 ? _options[0] : Cancel;
            first.Focus();
        }

        public void HideImmediate()
        {
            IsOpen = false;
            AdventurerId = -1;
            _onPick = null;
            _overlay.style.display = DisplayStyle.None;
            foreach (var option in _options)
                option.SetEnabled(false);
        }

        private void Pick(int index)
        {
            if (!IsOpen || index < 0 || index >= _shown.Count)
                return;

            var pick = _onPick;
            var chosen = _shown[index];
            HideImmediate();
            pick?.Invoke(chosen);
        }
    }
}