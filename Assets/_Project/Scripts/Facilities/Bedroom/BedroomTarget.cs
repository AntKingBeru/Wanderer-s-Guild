// A bedroom as a world hover/click target (no collider — picked by floor cell): label with beds, room outline, opens its card.
using UnityEngine;

namespace WanderersGuild
{
    public sealed class BedroomTarget : IWorldInteractable
    {
        private readonly PlacedFacility _room;
        private readonly Grid _grid;
        private bool _isHighlighted;

        public BedroomTarget(PlacedFacility room, Grid grid)
        {
            _room = room;
            _grid = grid;
        }

        public PlacedFacility Room => _room;

        // False once the room is gone (plain objects can't use Unity's destroyed-null).
        public bool IsValid => FacilityManager.HasInstance && LodgingRules.Contains(FacilityManager.Instance.Rooms, _room);

        // "Bedroom 2 · 3/4".
        public string DisplayName
        {
            get
            {
                var used = LodgingManager.HasInstance ? LodgingManager.Instance.Assignments.CountIn(_room.Id) : 0;
                return $"{LodgingRules.Label(_room, FacilityManager.Instance.Rooms)} · {used}/{LodgingRules.Beds(_room)}";
            }
        }

        // Above the middle of the room's footprint.
        public Vector3 LabelAnchor
        {
            get
            {
                var sum = Vector3.zero;
                var count = 0;
                foreach (var cell in _room.Cells)
                {
                    sum += _grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0)) + new Vector3(0.5f, 0f, 0.5f);
                    count++;
                }
                var centre = count > 0 ? sum / count : Vector3.zero;
                return centre + Vector3.up * (GameConfig.Instance.Avatars.WorldHeight + 0.5f);
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted)
                return;
            _isHighlighted = highlighted;

            if (highlighted)
                RoomHighlights.Add(_room);
            else
                RoomHighlights.Remove(_room);
        }

        public void Interact(GamePanelHost panels) => GameEventsRelay.OnRoomCardRequested.Invoke(_room.Id);
    }
}