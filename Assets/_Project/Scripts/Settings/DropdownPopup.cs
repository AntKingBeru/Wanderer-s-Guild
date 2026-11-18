// The open list of a pixel dropdown: items placed under their field in an overlay layer; a click anywhere outside closes it.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: The layer is an absolute, full-size element above the panel content, hidden until a list opens; while open it
    // swallows every click outside the list (that click only closes). Several dropdowns may share one layer — each shows only
    // its own list, and only one is ever open (the layer covers the other fields).
    public sealed class DropdownPopup
    {
        public const string ListUssClassName = "pixel-dropdown__list";
        public const string ItemUssClassName = "pixel-dropdown__item";
        public const string CurrentItemUssClassName = ItemUssClassName + "--current";

        private readonly VisualElement _field;
        private readonly VisualElement _layer;
        private readonly ScrollView _list;
        private readonly GameObject _linkTarget;
        private readonly Action _onOutsideClick;
        private readonly DropdownDirection _direction;
        private readonly float _minWidth;
        private readonly List<Button> _items = new();

        public bool IsOpen { get; private set; }

        // NOTE: The same list instance for the popup's life (rebuilt in place) — safe to hand to MenuNavigation once.
        public IReadOnlyList<Button> Items => _items;

        /// <param name="field"></param>
        /// <param name="layer"></param>
        /// <param name="linkTarget"></param>
        /// <param name="onOutsideClick"></param>
        /// <param name="direction">Down (under the field) or Up (above it — for fields near the bottom of a panel).</param>
        /// <param name="minWidth">The list is at least this wide (UI px), even under a narrower field.</param>
        public DropdownPopup(VisualElement field, VisualElement layer, GameObject linkTarget, Action onOutsideClick,
            DropdownDirection direction = DropdownDirection.Down, float minWidth = 0f)
        {
            _field = field;
            _layer = layer;
            _linkTarget = linkTarget;
            _onOutsideClick = onOutsideClick;
            _direction = direction;
            _minWidth = minWidth;

            _list = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            _list.AddToClassList(ListUssClassName);
            _list.AddToClassList("pixel-scroll"); // NOTE: Shared thin scrollbar from PixelTheme.uss.
            _list.style.position = Position.Absolute;
            _list.style.display = DisplayStyle.None;
            _layer.Add(_list);

            _layer.RegisterCallback<PointerDownEvent>(OnLayerPointerDown);
            _layer.style.display = DisplayStyle.None;
        }

        // Rebuilds the items; clicking one calls onClicked(index). leading (optional) adds an element before an item's label.
        public void SetItems(IReadOnlyList<string> labels, Action<int> onClicked, Func<int, VisualElement> leading = null)
        {
            _list.Clear();
            _items.Clear();

            for (var i = 0; i < labels.Count; i++)
            {
                var index = i;
                Button item = new(() =>
                {
                    if (IsOpen)
                        onClicked(index);
                });
                item.RemoveFromClassList(Button.ussClassName); // NOTE: Default theme styles would override our frames.
                item.AddToClassList("pixel-button");
                item.AddToClassList(ItemUssClassName);

                var lead = leading?.Invoke(i);
                if (lead != null)
                    item.Add(lead);
                item.Add(new OutlinedText { Text = labels[i] });
                item.RegisterCallback<FocusEvent>(_ => _list.ScrollTo(item));

                // NOTE: No pop. Items span the clipped list's full width, so a scale-up would push text past its edges.
                item.AddManipulator(new MenuButtonFeedback(_linkTarget, pop: false));

                _list.Add(item);
                _items.Add(item);
            }

            SetInteractable(false);
        }

        // Highlights the chosen item (single-choice lists).
        public void MarkCurrent(int index)
        {
            for (var i = 0; i < _items.Count; i++)
                _items[i].EnableInClassList(CurrentItemUssClassName, i == index);
        }

        public void Open(int focusIndex)
        {
            if (IsOpen || _items.Count == 0)
                return;

            IsOpen = true;
            _layer.style.display = DisplayStyle.Flex;
            _list.style.display = DisplayStyle.Flex;
            PositionList();
            SetInteractable(true);

            // NOTE: Focus next frame; the list was just displayed and hasn't been laid out yet.
            var target = _items[Mathf.Clamp(focusIndex, 0, _items.Count - 1)];
            target.schedule.Execute(() =>
            {
                if (!IsOpen)
                    return;
                target.Focus();
                _list.ScrollTo(target);
            });
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            SetInteractable(false);
            _list.style.display = DisplayStyle.None;
            _layer.style.display = DisplayStyle.None;
        }

        // NOTE: Under the field (Down) or above it (Up — placed by its bottom edge, so its height doesn't matter), in layer
        // space; at least as wide as the field. The layer fills its parent, so the parent's height is the layer's.
        private void PositionList()
        {
            var field = _field.worldBound;
            _list.style.width = Mathf.Max(field.width, _minWidth);

            if (_direction == DropdownDirection.Up)
            {
                var top = _layer.WorldToLocal(new Vector2(field.xMin, field.yMin));
                var height = _layer.parent != null ? _layer.parent.layout.height : 0f;
                _list.style.left = top.x;
                _list.style.top = StyleKeyword.Auto;
                _list.style.bottom = height - top.y;
                return;
            }

            var bottom = _layer.WorldToLocal(new Vector2(field.xMin, field.yMax));
            _list.style.left = bottom.x;
            _list.style.top = bottom.y;
            _list.style.bottom = StyleKeyword.Auto;
        }

        private void SetInteractable(bool interactable)
        {
            foreach (var item in _items)
            {
                item.focusable = interactable;
                item.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
            }
        }

        private void OnLayerPointerDown(PointerDownEvent evt)
        {
            if (!IsOpen)
                return;
            if (evt.target is VisualElement target && (target == _list || _list.Contains(target)))
                return;

            _onOutsideClick?.Invoke();
            evt.StopPropagation();
        }
    }
}