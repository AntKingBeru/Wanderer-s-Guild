// Every enabled activity spot, and which adventurer holds each (one per spot): reserve a free usable one, release by adventurer.
using System;
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Registry pattern (static) — spots live in spawned room prefabs, so there's no scene object to wire them to.
    public static class ActivitySpotRegistry
    {
        private static readonly List<ActivitySpot> Spots = new();
        private static readonly Dictionary<ActivitySpot, int> HeldBy = new();
        private static readonly List<ActivitySpot> Candidates = new();

        public static IReadOnlyList<ActivitySpot> All => Spots;

        public static void Register(ActivitySpot spot)
        {
            if (spot is not null && !Spots.Contains(spot))
                Spots.Add(spot);
        }

        // A vanished spot (room demolished / model swapped) drops its reservation; its avatar re-plans on the room change.
        public static void Unregister(ActivitySpot spot)
        {
            Spots.Remove(spot);
            HeldBy.Remove(spot);
        }

        // Reserves a random free spot of this kind that passes <paramref name="usable"/> (e.g. its cell is walkable).
        public static bool TryReserve(SpotKind kind, int adventurerId, Func<ActivitySpot, bool> usable, out ActivitySpot spot)
        {
            spot = null;
            Candidates.Clear();
            foreach (var candidate in Spots.Where(candidate => candidate is not null && candidate.Kind == kind && !HeldBy.ContainsKey(candidate))
                         .Where(candidate => usable == null || usable(candidate)))
            {
                Candidates.Add(candidate);
            }
            if (Candidates.Count == 0)
                return false;

            spot = Candidates[UnityEngine.Random.Range(0, Candidates.Count)];
            HeldBy[spot] = adventurerId;
            return true;
        }

        // Frees whatever spot this adventurer holds.
        public static void Release(int adventurerId)
        {
            var held = (from pair in HeldBy where pair.Value == adventurerId select pair.Key).FirstOrDefault();
            if (held is not null)
                HeldBy.Remove(held);
        }

        public static int CountOf(SpotKind kind)
        {
            return Spots.Count(spot => spot is not null && spot.Kind == kind);
        }

        // Scene teardown: clears reservations (spots unregister themselves as they're destroyed).
        public static void ClearReservations() => HeldBy.Clear();
    }
}