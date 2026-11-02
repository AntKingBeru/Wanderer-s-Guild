// The build-mode UI: catalog bar (a strip of cards scrolled sideways), controls hint, cursor label, build confirmation.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class BuildModeView
    {
        private const string InvalidClass = "build-cursor--invalid";
        private const float FallbackCardStep = 88f; // Card width 86 + margin 2, before the first layout.

        private readonly VisualElement _root;
        private readonly VisualElement _catalog;
        private readonly VisualElement _strip;
        private readonly VisualElement _cursor;
        private readonly OutlinedText _cursorText;
        private readonly InlineConfirmation _confirm;
        private readonly List<BuildCardView> _cards = new();
        private readonly Action<FacilityData> _onPicked;
        private int _visibleCards;
        private float _offset;

        private BuildModeView(VisualElement root, VisualElement catalog, VisualElement strip, VisualElement cursor,
                              OutlinedText cursorText, InlineConfirmation confirm, Action<FacilityData> onPicked)
        {
            _root = root;
            _catalog = catalog;
            _strip = strip;
            _cursor = cursor;
            _cursorText = cursorText;
            _confirm = confirm;
            _onPicked = onPicked;

            // NOTE: Re-clamp when the viewport resizes (window size, UI scale).
            _catalog.RegisterCallback<GeometryChangedEvent>(_ => SetOffset(_offset));
            _confirm.Initialize();
            _confirm.HideImmediate();
            Hide();
        }

        public bool IsConfirming => _confirm.IsOpen;

        public static BuildModeView Bind(VisualElement documentRoot, Action<FacilityData> onPicked)
        {
            var root = documentRoot?.Q<VisualElement>("build-root");
            var catalog = documentRoot?.Q<VisualElement>("build-catalog");
            var strip = documentRoot?.Q<VisualElement>("build-strip");
            var cursor = documentRoot?.Q<VisualElement>("build-cursor");
            var cursorText = documentRoot?.Q<OutlinedText>("build-cursor-text");
            var confirm = documentRoot != null ? InlineConfirmation.Bind(documentRoot) : null;

            return root != null && catalog != null && strip != null && cursor != null && cursorText != null && confirm != null
                ? new BuildModeView(root, catalog, strip, cursor, cursorText, confirm, onPicked)
                : null;
        }

        public void Show() => _root.style.display = DisplayStyle.Flex;

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
            HideCursor();
            if (_confirm.IsOpen)
                _confirm.HideImmediate();
        }

        // NOTE: Cards are pooled — created as needed, extra ones hidden. The strip's width is set explicitly to the cards'
        // total (the layout engine won't grow a container for overflowing children), so the scroll range is always right.
        public void ShowCatalog(IReadOnlyList<BuildCatalogEntry> entries, FacilityData selected)
        {
            while (_cards.Count < entries.Count)
            {
                BuildCardView card = new(_onPicked);
                _strip.Add(card.Button);
                _cards.Add(card);
            }

            var selectedIndex = -1;
            for (var i = 0; i < _cards.Count; i++)
            {
                if (i >= entries.Count)
                {
                    _cards[i].Hide();
                    continue;
                }

                var isSelected = entries[i].Data == selected;
                _cards[i].Show(entries[i], isSelected);
                if (isSelected)
                    selectedIndex = i;
            }

            _visibleCards = entries.Count;
            _strip.style.width = ContentWidth;
            if (selectedIndex >= 0)
                BringIntoView(selectedIndex);
            else
                SetOffset(_offset);
        }

        /// <summary>True if the screen position (Input System: y up from the bottom) is over the catalog.</summary>
        public bool IsOverCatalog(Vector2 screenPosition)
        {
            if (_catalog.panel == null || _root.resolvedStyle.display == DisplayStyle.None)
                return false;

            var panelPoint = RuntimePanelUtils.ScreenToPanel(_catalog.panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return _catalog.worldBound.Contains(panelPoint);
        }

        // Scrolls by whole cards (+ = right).
        public void ScrollCards(int cards) => SetOffset(_offset + cards * CardStep);

        /// <param name="text"></param>
        /// <param name="valid"></param>
        /// <param name="localPosition">Pointer position in the canvas (world-view local space).</param>
        public void ShowCursor(string text, bool valid, Vector2 localPosition)
        {
            _cursorText.Text = text;
            _cursor.EnableInClassList(InvalidClass, !valid);
            _cursor.style.left = Mathf.Round(localPosition.x);
            _cursor.style.top = Mathf.Round(localPosition.y);
            _cursor.style.display = DisplayStyle.Flex;
        }

        public void HideCursor() => _cursor.style.display = DisplayStyle.None;

        public void Confirm(string message, Action onYes, Action onClosed) => _confirm.Show(message, onYes, onClosed);

        public void CancelConfirm()
        {
            if (_confirm.IsOpen)
                _confirm.Cancel();
        }

        // ---------- Scrolling ----------

        // >One card plus its right margin, from the cards' resolved style (fallback before the first layout).
        private float CardStep
        {
            get
            {
                foreach (var width in from card in _cards
                         where card.Data select card.Button.resolvedStyle.width + card.Button.resolvedStyle.marginRight into width
                         where !float.IsNaN(width) && width > 0f select width)
                {
                    return width;
                }

                return FallbackCardStep;
            }
        }

        private float ContentWidth => _visibleCards * CardStep;

        private float ViewportWidth
        {
            get
            {
                var width = _catalog.contentRect.width;
                return float.IsNaN(width) ? 0f : width;
            }
        }

        // NOTE: Card "i" spans [i·step, i·step + step]; scroll just enough to show all of it.
        private void BringIntoView(int index)
        {
            float step = CardStep, viewport = ViewportWidth;
            float left = index * step, right = left + step;

            if (left < _offset)
                SetOffset(left);
            else if(viewport > 0f && right > _offset + viewport)
                SetOffset(right - viewport);
            else
                SetOffset(_offset);
        }

        // NOTE: Clamped to [0, content − viewport] and moved in whole pixels.
        private void SetOffset(float offset)
        {
            var max = Mathf.Max(0f, ContentWidth - ViewportWidth);
            _offset = Mathf.Clamp(offset, 0f, max);
            _strip.style.translate = new Translate(-Mathf.Round(_offset), 0f);
        }
    }
}