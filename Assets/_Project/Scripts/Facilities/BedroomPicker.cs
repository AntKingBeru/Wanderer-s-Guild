// Finds the bedroom under the pointer by its floor cell (rooms with beds at their current level), one cached target per room.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class BedroomPicker
    {
        private readonly Dictionary<int, BedroomTarget> _targets = new();
        private readonly Grid _grid;

        public BedroomPicker(Grid grid) => _grid = grid;

        // NOTE: Cached per room id, so the same room is the same target every frame (no hover flicker).
        public BedroomTarget Find(Camera camera, WorldViewPresenter view, Vector2 screen)
        {
            if (!_grid || !FacilityManager.HasInstance)
                return null;

            var floor = GameConfig.Instance.BuildMode.FloorHeight;
            if (!BuildPointer.TryFloorPoint(camera, view, screen, floor, out var point, out _))
                return null;

            var room = FacilityManager.Instance.RoomAt(BuildPointer.CellAt(_grid, point));
            if (room == null || LodgingRules.Beds(room) <= 0)
                return null;

            if (!_targets.TryGetValue(room.Id, out var target) || target.Room != room)
                _targets[room.Id] = target = new BedroomTarget(room, _grid);
            return target;
        }

        public void Forget(PlacedFacility room)
        {
            if (room != null)
                _targets.Remove(room.Id);
        }
    }
}