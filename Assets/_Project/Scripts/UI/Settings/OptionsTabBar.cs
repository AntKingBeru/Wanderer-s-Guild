using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class OptionsTabBar
    {
        private const string RowName = "options-tabs";
        private const string SelectedClass = "options-tab--selected";

        private readonly VisualElement _row;
        private readonly List<SettingsTab> _order = new();
        private readonly List<Button> _buttons = new();
        private readonly Dictionary<SettingsTab, Button> _buttonsByTab = new();
        private readonly Dictionary<SettingsTab, VisualElement> _pages = new();
        private Action<SettingsTab> _onSelected;
        private Func<bool> _isActive;
        private Func<SettingsTab, bool> _canSelect;

        public SettingsTab Selected { get; private set; }
        public Button SelectedButton => _buttonsByTab[Selected];
        public IReadOnlyList<Button> Buttons => _buttons;

        private OptionsTabBar(VisualElement row) => _row = row;

        // Finds the tab row, tab buttons and pages. Returns null if anything is missing.
        public static OptionsTabBar Bind(VisualElement root)
        {
            var row = root.Q<VisualElement>(RowName);
            if (row == null)
                return null;

            OptionsTabBar bar = new(row);
            foreach (var tab in (SettingsTab[])Enum.GetValues(typeof(SettingsTab)))
            {
                var button = root.Q<Button>(ButtonName(tab));
                var page = root.Q<VisualElement>(PageName(tab));
                if (button == null || page == null) return null;

                bar._order.Add(tab);
                bar._buttons.Add(button);
                bar._buttonsByTab[tab] = button;
                bar._pages[tab] = page;
            }
            return bar;
        }

        /// <param name="onSelected"></param>
        /// <param name="isActive"></param>
        /// <param name="canSelect">Asked before a player-driven switch. Returning false holds it back (the owner may
        /// ask the player something, then call Select itself).</param>
        public void Initialize(Action<SettingsTab> onSelected, Func<bool> isActive, Func<SettingsTab, bool> canSelect = null)
        {
            _onSelected = onSelected;
            _isActive = isActive;
            _canSelect = canSelect;

            foreach (var tab in _order)
                _buttonsByTab[tab].clicked += () => TrySelect(tab);

            _row.RegisterCallback<NavigationMoveEvent>(OnMove);
        }

        // Switches immediately (no guard). Used by the owner, e.g. when a panel opens.
        public void Select(SettingsTab tab)
        {
            Selected = tab;

            foreach (var candidate in _order)
            {
                var isSelected = candidate == tab;
                _buttonsByTab[candidate].EnableInClassList(SelectedClass, isSelected);
                _pages[candidate].style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _onSelected?.Invoke(tab);
        }

        private bool TrySelect(SettingsTab tab)
        {
            if (tab == Selected)
                return true;
            if (_canSelect != null && !_canSelect(tab))
                return false;

            Select(tab);
            return true;
        }

        // NOTE: Reaches here only while a tab button is focused (the event bubbles up from it).
        private void OnMove(NavigationMoveEvent evt)
        {
            if (_isActive == null || !_isActive())
                return;

            var step = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => -1,
                NavigationMoveEvent.Direction.Right => 1,
                _ => 0
            };
            if (step == 0)
                return;

            var index = (_order.IndexOf(Selected) + step + _order.Count) % _order.Count;
            if (TrySelect(_order[index]))
                SelectedButton.Focus();

            evt.StopPropagation();
            _row.focusController?.IgnoreEvent(evt);
        }

        private static string ButtonName(SettingsTab tab) => tab switch
        {
            SettingsTab.Audio => "tab-audio",
            SettingsTab.Display => "tab-display",
            SettingsTab.Controls => "tab-controls",
            _ => string.Empty
        };

        private static string PageName(SettingsTab tab) => tab switch
        {
            SettingsTab.Audio => "tab-page-audio",
            SettingsTab.Display => "tab-page-display",
            SettingsTab.Controls => "tab-page-controls",
            _ => string.Empty
        };
    }
}