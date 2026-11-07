// The floating room card in the world view: "Bedroom 2 · Lv 2", beds used, and the residents' names (pooled lines).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RoomLodgingCardView
    {
        private const int MaxLines = 8;

        private readonly VisualElement _root;
        private readonly OutlinedText _title;
        private readonly OutlinedText _beds;
        private readonly OutlinedText _empty;
        private readonly List<OutlinedText> _lines = new();

        private RoomLodgingCardView(VisualElement root, OutlinedText title, OutlinedText beds, OutlinedText empty,
                                    VisualElement list, Button close, Action onClose)
        {
            _root = root;
            _title = title;
            _beds = beds;
            _empty = empty;

            for (var i = 0; i < MaxLines; i++)
            {
                OutlinedText line = new() { pickingMode = PickingMode.Ignore };
                line.AddToClassList("world-card__line");
                list.Add(line);
                _lines.Add(line);
            }

            // NOTE: Never focusable — Space is UI Submit and the time-pause hotkey.
            close.focusable = false;
            close.clicked += onClose;
            Hide();
        }

        public static RoomLodgingCardView Bind(VisualElement documentRoot, Action onClose)
        {
            var root = documentRoot?.Q<VisualElement>("room-card");
            var title = documentRoot?.Q<OutlinedText>("room-card-title");
            var beds = documentRoot?.Q<OutlinedText>("room-card-beds");
            var empty = documentRoot?.Q<OutlinedText>("room-card-empty");
            var list = documentRoot?.Q<VisualElement>("room-card-residents");
            var close = documentRoot?.Q<Button>("room-card-close");

            return root != null && title != null && beds != null && empty != null && list != null && close != null
                ? new RoomLodgingCardView(root, title, beds, empty, list, close, onClose)
                : null;
        }

        /// <param name="title"></param>
        /// <param name="used"></param>
        /// <param name="beds"></param>
        /// <param name="residents">Names; more than the pooled lines end with "+N more".</param>
        public void SetContent(string title, int used, int beds, IReadOnlyList<string> residents)
        {
            _title.Text = title;
            _beds.Text = $"Beds {used}/{beds}";
            _empty.style.display = residents.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            var overflow = residents.Count > MaxLines;
            var shown = overflow ? MaxLines - 1 : residents.Count;
            for (var i = 0; i < _lines.Count; i++)
            {
                var text = i < shown ? residents[i] : overflow && i == shown ? $"+{residents.Count - shown} more" : null;
                _lines[i].Text = text ?? string.Empty;
                _lines[i].style.display = text != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // Places the card's top-left at a local position, clamped inside the area (whole pixels).
        public void Place(Vector2 localPosition, Rect area, float topMargin)
        {
            float width = _root.resolvedStyle.width, height = _root.resolvedStyle.height;
            var p = localPosition;
            if (!float.IsNaN(width) && !float.IsNaN(height))
            {
                p.x = Mathf.Clamp(p.x - width * 0.5f, area.xMin, Mathf.Max(area.xMin, area.xMax - width));
                p.y = Mathf.Clamp(p.y - height, area.yMin + topMargin, Mathf.Max(area.yMin + topMargin, area.yMax - height));
            }

            _root.style.left = Mathf.Round(p.x);
            _root.style.top = Mathf.Round(p.y);
        }

        public void Show() => _root.style.display = DisplayStyle.Flex;
        public void Hide() => _root.style.display = DisplayStyle.None;
    }
}