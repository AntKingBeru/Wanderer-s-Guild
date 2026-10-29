using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [UxmlElement]
    public sealed partial class PixelDropdown : VisualElement
    {
        public const string UssClassName = "pixel-dropdown";
        public const string ValueUssClassName = UssClassName + "__value";
        public const string ArrowUssClassName = UssClassName + "__arrow";
        public const string ListUssClassName = UssClassName + "__list";
        public const string ItemUssClassName = UssClassName + "__item";

        private readonly OutlinedText _value;
        private readonly List<string> _options = new();
        private readonly List<Button> _items = new();
        private VisualElement _layer;
        private ScrollView _list;
        private GameObject _linkTarget;
        private Action<int> _onChanged;

        public int Index { get; private set; }
        public bool IsOpen { get; private set; }

        // The open list's items, in order (for keyboard navigation while open).
        public IReadOnlyList<Button> Items => _items;

        public PixelDropdown()
        {
            AddToClassList(UssClassName);
            focusable = true;

            _value = new OutlinedText();
            _value.AddToClassList(ValueUssClassName);
            Add(_value);

            OutlinedText arrow = new() { Text = "v" };
            arrow.AddToClassList(ArrowUssClassName);
            Add(arrow);

            RegisterCallback<ClickEvent>(_ => Open());
            RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            RegisterCallback<PointerEnterEvent>(_ => { if (enabledInHierarchy && focusable && !IsOpen) Focus(); });
            RegisterCallback<FocusEvent>(_ => GameEventsRelay.OnUIButtonFocused.Invoke());
        }

        /// <param name="layer">An absolute, full-size element drawn above the panel content. The list is placed inside it.</param>
        /// <param name="linkTarget">GameObject the item tweens are linked to.</param>
        public void Initialize(VisualElement layer, GameObject linkTarget)
        {
            _layer = layer;
            _linkTarget = linkTarget;

            _list = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            _list.AddToClassList(ListUssClassName);
            _list.AddToClassList("pixel-scroll"); // NOTE: Shared thin scrollbar from PixelTheme.uss.
            _list.style.position = Position.Absolute;
            _layer.Add(_list);

            // NOTE: A click anywhere outside the list (including on this field) closes it.
            _layer.RegisterCallback<PointerDownEvent>(OnLayerPointerDown);
            _layer.style.display = DisplayStyle.None;
        }

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
            _value.Text = _options.Count == 0 ? string.Empty : _options[Index];
        }

        public void Open()
        {
            if (IsOpen || _layer == null || _options.Count == 0 || !enabledInHierarchy)
                return;

            IsOpen = true;
            _layer.style.display = DisplayStyle.Flex;
            PositionList();
            SetItemsInteractable(true);
            GameEventsRelay.OnUIButtonClicked.Invoke();

            // NOTE: Focus next frame; the list was just displayed and hasn't been laid out yet.
            var current = _items[Index];
            current.schedule.Execute(() =>
            {
                if (!IsOpen)
                    return;
                current.Focus();
                _list.ScrollTo(current);
            });
        }

        public void Close(bool refocus = true)
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            SetItemsInteractable(false);
            _layer.style.display = DisplayStyle.None;

            if (refocus) Focus();
        }

        private void RebuildItems()
        {
            if (_list == null)
                return;

            _list.Clear();
            _items.Clear();

            for (var i = 0; i < _options.Count; i++)
            {
                var index = i;
                Button item = new(() => Choose(index));
                item.RemoveFromClassList(Button.ussClassName); // NOTE: Default theme styles would override our frames.
                item.AddToClassList("pixel-button");
                item.AddToClassList(ItemUssClassName);
                item.Add(new OutlinedText { Text = _options[i] });
                item.RegisterCallback<FocusEvent>(_ => _list.ScrollTo(item));

                // NOTE: No pop. Items span the clipped list's full width, so a scale-up would push text past its edges.
                item.AddManipulator(new MenuButtonFeedback(_linkTarget, pop: false));

                _list.Add(item);
                _items.Add(item);
            }

            SetItemsInteractable(false);
        }

        private void Choose(int index)
        {
            if (!IsOpen)
                return;

            SetIndexWithoutNotify(index);
            Close();
            _onChanged?.Invoke(Index);
        }

        // NOTE: Places the list just under the field, in layer space (works inside scrolled pages and at any UI scale).
        private void PositionList()
        {
            var field = worldBound;
            var topLeft = _layer.WorldToLocal(new Vector2(field.xMin, field.yMax));
            _list.style.left = topLeft.x;
            _list.style.top = topLeft.y;
            _list.style.width = field.width;
        }

        private void SetItemsInteractable(bool interactable)
        {
            foreach (var item in _items)
            {
                item.focusable = interactable;
                item.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
            }
        }

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            if (IsOpen)
                return;

            Open();
            evt.StopPropagation();
        }

        private void OnLayerPointerDown(PointerDownEvent evt)
        {
            if (!IsOpen)
                return;
            if (evt.target is VisualElement target && (target == _list || _list.Contains(target)))
                return;

            Close();
            evt.StopPropagation();
        }
    }
}