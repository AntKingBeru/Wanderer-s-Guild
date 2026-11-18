// Shared field of the pixel dropdowns: a framed value and a "v" arrow that opens the dropdown's list (DropdownPopup).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Template method — the field, opening, closing and Submit live here; each dropdown decides its items and which one
    // is focused when the list opens.
    public abstract class PixelDropdownBase : VisualElement
    {
        public const string UssClassName = "pixel-dropdown";
        public const string ValueUssClassName = UssClassName + "__value";
        public const string ArrowUssClassName = UssClassName + "__arrow";

        private readonly OutlinedText _value;

        protected DropdownPopup Popup { get; private set; }
        protected abstract int FocusIndex { get; }

        public bool IsOpen => Popup is { IsOpen: true };

        // The list's items, in order (keyboard navigation while open). Empty until Initialize.
        public IReadOnlyList<Button> Items => Popup != null ? Popup.Items : Array.Empty<Button>();

        protected PixelDropdownBase()
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

        /// <param name="layer">An absolute, full-size element drawn above the panel content (can be shared).</param>
        /// <param name="linkTarget">GameObject the item tweens are linked to.</param>
        public void Initialize(VisualElement layer, GameObject linkTarget)
        {
            Popup = new DropdownPopup(this, layer, linkTarget, () => Close());
            OnInitialized();
        }

        public void Open()
        {
            if (IsOpen || Popup == null || Popup.Items.Count == 0 || !enabledInHierarchy)
                return;

            Popup.Open(FocusIndex);
            GameEventsRelay.OnUIButtonClicked.Invoke();
        }

        public void Close(bool refocus = true)
        {
            if (!IsOpen)
                return;

            Popup.Close();
            if (refocus)
                Focus();
        }

        // NOTE: Builds any items set before Initialize (they had no list to go in yet).
        protected abstract void OnInitialized();

        protected void SetValueText(string text) => _value.Text = text ?? string.Empty;

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            if (IsOpen)
                return;

            Open();
            evt.StopPropagation();
        }
    }
}