// Facility rules (pure): may this room be placed here, upgraded or demolished now — and at what cost or refund.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class FacilityRules
    {
        private static readonly List<int> Linked = new();

        // NOTE: Order = what the player most needs to know first: rank, limit, space, connection, gold.
        public static FacilityCheck CanPlace(FacilityData data, Vector2Int origin, int rotation, FacilityGrid grid,
                                             IReadOnlyList<PlacedFacility> rooms, GuildRank guildRank, long gold, FacilitySettings settings)
        {
            var lockReason = LockReason(data, rooms, guildRank);
            if (lockReason != null)
                return FacilityCheck.No(lockReason);

            var geometry = GeometryReason(data, origin, rotation, grid, settings);
            if (geometry != null)
                return FacilityCheck.No(geometry);

            var cost = data.CostFor(1);
            return gold < cost ? FacilityCheck.No($"Costs {cost:N0}g: not enough gold", cost) : FacilityCheck.Ok(cost);
        }

        // Position-independent reasons a facility can't be built at all right now (rank, limit), or null.
        public static string LockReason(FacilityData data, IReadOnlyList<PlacedFacility> rooms, GuildRank guildRank)
        {
            var unlock = data.UnlockRankFor(1);
            if (unlock > guildRank)
                return $"Needs guild rank {unlock}";
            if (data.Limit > 0 && CountOf(data, rooms) >= data.Limit)
                return $"Limit reached ({data.Limit})";
            return null;
        }

        // Why this spot doesn't work (outside the area, overlap, not connected), or null if it fits.
        public static string GeometryReason(FacilityData data, Vector2Int origin, int rotation, FacilityGrid grid, FacilitySettings settings)
        {
            var area = settings.BuildArea;
            var size = FacilityFootprint.RotatedSize(data.Size, rotation);
            if (origin.x < area.xMin || origin.y < area.yMin || origin.x + size.x > area.xMax || origin.y + size.y > area.yMax)
                return "Outside the build area";

            if (!grid.IsFree(FacilityFootprint.Cells(origin, data.Size, rotation)))
                return "Overlaps another room";

            grid.LinkedRooms(WorldDoors(data, origin, rotation), -1, Linked);
            return Linked.Count == 0 ? "Not connected to the guild" : null;
        }

        public static FacilityCheck CanUpgrade(PlacedFacility room, GuildRank guildRank, long gold)
        {
            if (room.IsUnderConstruction)
                return FacilityCheck.No("Under construction");
            if (room.Level >= room.Data.MaxLevel)
                return FacilityCheck.No("Highest level");

            var next = room.Level + 1;
            var unlock = room.Data.UnlockRankFor(next);
            if (unlock > guildRank)
                return FacilityCheck.No($"Needs guild rank {unlock}");

            var cost = room.Data.CostFor(next);
            return gold < cost ? FacilityCheck.No($"Costs {cost:N0}g: not enough gold", cost) : FacilityCheck.Ok(cost);
        }

        public static FacilityCheck CanDemolish(PlacedFacility room, FacilityGrid grid, int hallId, FacilitySettings settings)
        {
            if (room.Id == hallId) return FacilityCheck.No("The Guild Hall can't be demolished");
            return !grid.StaysConnectedWithout(room.Id, hallId)
                ? FacilityCheck.No("Other rooms depend on it")
                : FacilityCheck.Ok((long)(room.TotalSpent * settings.DemolishRefund));
        }

        public static int CountOf(FacilityData data, IReadOnlyList<PlacedFacility> rooms)
        {
            return rooms.Count(room => room.Data == data);
        }

        public static IEnumerable<WorldDoor> WorldDoors(FacilityData data, Vector2Int origin, int rotation)
        {
            return data.Connectors.Select(connector => FacilityFootprint.ToWorldDoor(connector, data.Size, origin, rotation));
        }
    }
}