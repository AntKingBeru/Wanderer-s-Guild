// Reads facility effects across the guild's rooms (finished levels only): total capacity and one room's share.
using System.Collections.Generic;

namespace WanderersGuild
{
    public static class FacilityEffects
    {
        // Total beds from every room's CapacityEffect at its finished level; <c>anyProvider</c> is false when no
        // facility has a capacity effect at all (the caller then uses the fallback).
        public static (int total, bool anyProvider) Capacity(IReadOnlyList<PlacedFacility> rooms)
        {
            var total = 0;
            var any = false;
            foreach (var room in rooms)
            {
                CapacityEffect effect = room.Data.Effect<CapacityEffect>();
                if (!effect)
                    continue;

                any = true;
                total += effect.CapacityAt(room.Level);
            }
            return (total, any);
        }

        // The beds this room provides right now (its finished level).
        public static int CapacityOf(PlacedFacility room)
        {
            CapacityEffect effect = room.Data.Effect<CapacityEffect>();
            return effect ? effect.CapacityAt(room.Level) : 0;
        }
    }
}