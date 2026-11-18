// The roster's Room list: the selected adventurer's bedroom and every other with a free bed, opening above the Room button;
// picking one moves them there.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: The choices are rebuilt every time it opens (beds change all day), never while it's open; LodgingManager
    // re-checks the pick.
    public sealed class RoomPickerView
    {
        private const float MinListWidth = 90f; // NOTE: "Bedroom 12 · 3/4" (16 characters) + padding and border.

        private readonly Button _button;
        private readonly DropdownPopup _popup;
        private readonly List<PlacedFacility> _rooms = new();
        private readonly List<string> _labels = new();
        private readonly Action<int, int> _onPicked;
        private int _adventurerId = -1;

        public bool IsOpen => _popup.IsOpen;

        // NOTE: The same list instance for the picker's life — safe to hand to MenuNavigation once.
        public IReadOnlyList<Button> Items => _popup.Items;

        /// <param name="button">The Room button (the list opens above it).</param>
        /// <param name="layer">The panel's dropdown layer (shared with Sort / Filter).</param>
        /// <param name="linkTarget"></param>
        /// <param name="onPicked">(adventurer id, room id).</param>
        public RoomPickerView(Button button, VisualElement layer, GameObject linkTarget, Action<int, int> onPicked)
        {
            _button = button;
            _onPicked = onPicked;
            _popup = new DropdownPopup(button, layer, linkTarget, () => Close(true), DropdownDirection.Up, MinListWidth);
        }

        // Opens this adventurer's choices; their current room is highlighted and focused. Nothing to show → stays closed.
        public void Open(int adventurerId)
        {
            if (IsOpen || !LodgingManager.HasInstance || !FacilityManager.HasInstance)
                return;

            var lodging = LodgingManager.Instance;
            lodging.RoomChoicesFor(adventurerId, _rooms);
            if (_rooms.Count == 0)
                return;

            _adventurerId = adventurerId;
            var current = lodging.RoomOf(adventurerId);
            var currentIndex = -1;
            _labels.Clear();
            for (var i = 0; i < _rooms.Count; i++)
            {
                _labels.Add(LodgingRules.BedsLabel(_rooms[i], FacilityManager.Instance.Rooms, lodging.Assignments));
                if (_rooms[i].Id == current)
                    currentIndex = i;
            }

            _popup.SetItems(_labels, Pick);
            _popup.MarkCurrent(currentIndex);
            _popup.Open(Mathf.Max(0, currentIndex));
        }

        public void Close(bool refocus)
        {
            if (!IsOpen)
                return;

            _popup.Close();
            if (refocus)
                _button.Focus();
        }

        private void Pick(int index)
        {
            var roomId = _rooms[index].Id;
            Close(true);
            _onPicked?.Invoke(_adventurerId, roomId);
        }
    }
}