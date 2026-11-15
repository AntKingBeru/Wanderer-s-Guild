// Infirmary care: whether a world point lies in an infirmary, and how fast an adventurer recovers right now — faster only
// while they're lying in one of its beds.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: "Lying there" = the avatar has reached a Bed spot inside an infirmary (ActivitySpotRegistry.TryGetReached) — not
    // while walking to it, and never for someone away or without an avatar. One bed, one room: infirmaries can't stack.
    public static class InfirmaryCare
    {
        // Recovery speed for this adventurer right now (1 = normal).
        public static float SpeedFor(int adventurerId)
        {
            if (!ActivitySpotRegistry.TryGetReached(adventurerId, out var spot) || spot.Kind != SpotKind.Bed)
                return 1f;

            var room = RoomAt(spot.Position);
            var effect = room?.Data.Effect<InfirmaryEffect>();
            return effect != null ? effect.RecoverySpeedAt(room.Level) : 1f;
        }

        // True if this point is inside a room with an infirmary effect (its beds are for patients).
        public static bool IsInfirmaryAt(Vector3 worldPosition)
        {
            var room = RoomAt(worldPosition);
            return room != null && room.Data.Effect<InfirmaryEffect>() != null;
        }

        private static PlacedFacility RoomAt(Vector3 worldPosition) =>
            FacilityManager.HasInstance ? FacilityManager.Instance.RoomAtWorld(worldPosition) : null;
    }
}