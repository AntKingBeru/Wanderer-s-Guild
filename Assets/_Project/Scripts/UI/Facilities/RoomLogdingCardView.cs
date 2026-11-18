// The floating room card in the world view: title, beds used, residents' names, and "+ Name" buttons that move in members
// who need a room (all lines pooled).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RoomLodgingCardView
    {
        private const int MaxLines = 8;
        private const int MaxAssign = 4;

        private readonly VisualElement _root;
        private readonly OutlinedText _title;
        private readonly OutlinedText _beds;
        private readonly OutlinedText _empty;
        private readonly VisualElement _assign;
        private readonly OutlinedText _assignMore;
        private readonly List<OutlinedText> _lines = new();
        private readonly List<Button> _assignButtons = new();
        private readonly List<OutlinedText> _assignLabels = new();
        private readonly int[] _assignIds = new int[MaxAssign];

        private RoomLodgingCardView(VisualElement root, OutlinedText title, OutlinedText beds, OutlinedText empty,
                                    VisualElement list, VisualElement assign, Button close, Action onClose, Action<int> onAssign)
        {
            _root = root;
            _title = title;
            _beds = beds;
            _empty = empty;
            _assign = assign;

            for (var i = 0; i < MaxLines; i++)
            {
                OutlinedText line = new() { pickingMode = PickingMode.Ignore };
                line.AddToClassList("world-card__line");
                list.Add(line);
                _lines.Add(line);
            }

            _assignMore = BuildAssignSection(onAssign);

            // NOTE: Never focusable — Space is UI Submit and the time-pause hotkey.
            close.focusable = false;
            close.clicked += onClose;
            Hide();
        }

        /// <param name="documentRoot"></param>
        /// <param name="onClose"></param>
        /// <param name="onAssign">Called with the adventurer id of a "+ Name" button.</param>
        public static RoomLodgingCardView Bind(VisualElement documentRoot, Action onClose, Action<int> onAssign)
        {
            var root = documentRoot?.Q<VisualElement>("room-card");
            var title = documentRoot?.Q<OutlinedText>("room-card-title");
            var beds = documentRoot?.Q<OutlinedText>("room-card-beds");
            var empty = documentRoot?.Q<OutlinedText>("room-card-empty");
            var list = documentRoot?.Q<VisualElement>("room-card-residents");
            var assign = documentRoot?.Q<VisualElement>("room-card-assign");
            var close = documentRoot?.Q<Button>("room-card-close");

            return root != null && title != null && beds != null && empty != null && list != null && assign != null &&
                   close != null
                ? new RoomLodgingCardView(root, title, beds, empty, list, assign, close, onClose, onAssign)
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

        // NOTE: Shown only with a free bed and someone lodging outside; past the pooled buttons, "+N more (roster)".
        public void SetAssignable(IReadOnlyList<Adventurer> outsiders, bool hasFreeBed)
        {
            var show = hasFreeBed && outsiders.Count > 0;
            _assign.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show)
                return;

            var shown = Mathf.Min(outsiders.Count, MaxAssign);
            for (var i = 0; i < _assignButtons.Count; i++)
            {
                var visible = i < shown;
                _assignButtons[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible)
                    continue;

                _assignIds[i] = outsiders[i].Id;
                _assignLabels[i].Text = $"+ {outsiders[i].Name}";
            }

            var more = outsiders.Count - shown;
            _assignMore.Text = more > 0 ? $"+{more} more (roster)" : string.Empty;
            _assignMore.style.display = more > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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

        // NOTE: "Needs a room:", the pooled "+ Name" buttons (never focusable — Space is the pause hotkey), then the overflow
        // line. Each button reads its id when clicked, so the pool is reused as the queue changes.
        private OutlinedText BuildAssignSection(Action<int> onAssign)
        {
            OutlinedText header = new() { Text = "Needs a room:", pickingMode = PickingMode.Ignore };
            header.AddToClassList("world-card__line");
            _assign.Add(header);

            for (var i = 0; i < MaxAssign; i++)
            {
                var index = i;
                Button button = new(() => onAssign?.Invoke(_assignIds[index])) { focusable = false };
                button.RemoveFromClassList(Button.ussClassName); // NOTE: Default theme styles would override our frames.
                button.AddToClassList("pixel-button");
                button.AddToClassList("room-card__assign-button");

                OutlinedText label = new();
                button.Add(label);
                _assign.Add(button);
                _assignButtons.Add(button);
                _assignLabels.Add(label);
            }

            OutlinedText more = new() { pickingMode = PickingMode.Ignore };
            more.AddToClassList("world-card__line");
            _assign.Add(more);
            _assign.style.display = DisplayStyle.None;
            return more;
        }
    }
}