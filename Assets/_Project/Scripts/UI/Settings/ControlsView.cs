// Options › Controls: a read-only list of what each key or mouse button does (GameConfig › Controls), one row per entry.
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Rows are focusable only so the keyboard can scroll the list (the panel scrolls a focused row into view). Keys come
    // from each entry (ControlEntry.KeysText — the action's own bindings unless overridden).
    public sealed class ControlsView
    {
        // NOTE: The Controls page IS the scroll view (like the Display page) — a scroll view nested in a flex-basis-0 page can
        // collapse to 0 height.
        private const string ListName = "tab-page-controls";

        private readonly List<VisualElement> _rows = new();

        public ScrollView List { get; }
        public IReadOnlyList<VisualElement> Focusables => _rows;

        private ControlsView(ScrollView list) => List = list;

        public static ControlsView Bind(VisualElement root)
        {
            var list = root.Q<ScrollView>(ListName);
            return list != null ? new ControlsView(list) : null;
        }

        // NOTE: Built once (Options initialization); rebuild after rebinding when that exists.
        public void Build(ControlsSettings settings)
        {
            List.Clear();
            _rows.Clear();

            foreach (var entry in settings.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Label))
                    continue;

                VisualElement row = new() { focusable = true };
                row.AddToClassList("controls-row");
                row.Add(Text("controls-row__label", entry.Label));
                row.Add(Text("controls-row__keys", entry.KeysText()));
                List.Add(row);
                _rows.Add(row);
            }

            if (_rows.Count == 0)
                List.Add(Text("options-placeholder", "No controls listed."));
        }

        private static OutlinedText Text(string className, string text)
        {
            OutlinedText element = new() { Text = text };
            element.AddToClassList(className);
            return element;
        }
    }
}