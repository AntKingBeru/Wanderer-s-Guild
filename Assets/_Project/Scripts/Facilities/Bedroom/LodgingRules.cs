// Lodging rules (pure): beds and free beds per room, the best room for a newcomer, and room labels ("Bedroom 2").
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public static class LodgingRules
    {
        public static int Beds(PlacedFacility room) => FacilityEffects.CapacityOf(room);

        public static int FreeBeds(PlacedFacility room, LodgingAssignments assignments) =>
            System.Math.Max(0, Beds(room) - assignments.CountIn(room.Id));

        // The room with the most free beds (ties: the oldest room). False if no bed is free anywhere.
        public static bool TryBestRoom(IReadOnlyList<PlacedFacility> rooms, LodgingAssignments assignments, out PlacedFacility best)
        {
            best = null;
            var bestFree = 0;
            foreach (var room in rooms)
            {
                var free = FreeBeds(room, assignments);
                if (free <= 0)
                    continue;
                if (best != null && (free < bestFree || (free == bestFree && room.Id > best.Id)))
                    continue;

                best = room;
                bestFree = free;
            }
            return best != null;
        }

        // "Bedroom 2": the facility's name + its order (by id) among rooms of that type.
        public static string Label(PlacedFacility room, IReadOnlyList<PlacedFacility> rooms)
        {
            var index = 1 + rooms.Count(other => other.Data == room.Data && other.Id < room.Id);
            return $"{room.Data.DisplayName} {index}";
        }
        
        // "Bedroom 2 · 3/4": the room's label + beds used / beds (hover label, roster room list).
        public static string BedsLabel(PlacedFacility room, IReadOnlyList<PlacedFacility> rooms, LodgingAssignments assignments) =>
            $"{Label(room, rooms)} · {assignments.CountIn(room.Id)}/{Beds(room)}";
        
        // The room with this id, or null. (Rooms are exposed read-only, so there's no List.Find.)
        public static PlacedFacility FindRoom(IReadOnlyList<PlacedFacility> rooms, int roomId)
        {
            return rooms.FirstOrDefault(room => room.Id == roomId);
        }

        // True if this exact room is still in the list (i.e. it hasn't been removed).
        public static bool Contains(IReadOnlyList<PlacedFacility> rooms, PlacedFacility room)
        {
            return room != null && rooms.Any(other => other == room);
        }
    }
}