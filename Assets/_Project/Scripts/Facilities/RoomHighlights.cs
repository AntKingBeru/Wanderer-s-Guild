// Shared room outlines for the world cards and hover: counts requests per room so one owner never clears another's outline.
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Build mode uses FacilityManager.SetHighlighted directly; world hover/cards are off in build mode, so the two never mix.
    public static class RoomHighlights
    {
        private static readonly Dictionary<int, int> Counts = new();

        public static void Add(PlacedFacility room)
        {
            if (room == null || !FacilityManager.HasInstance)
                return;

            var count = Counts.TryGetValue(room.Id, out var c) ? c + 1 : 1;
            Counts[room.Id] = count;
            if (count == 1)
                FacilityManager.Instance.SetHighlighted(room, true);
        }

        public static void Remove(PlacedFacility room)
        {
            if (room == null || !Counts.TryGetValue(room.Id, out var count))
                return;

            if (--count > 0)
            {
                Counts[room.Id] = count;
                return;
            }

            Counts.Remove(room.Id);
            if (FacilityManager.HasInstance)
                FacilityManager.Instance.SetHighlighted(room, false);
        }

        // A removed room's count is forgotten (its views are already gone).
        public static void Forget(PlacedFacility room)
        {
            if (room != null)
                Counts.Remove(room.Id);
        }
    }
}