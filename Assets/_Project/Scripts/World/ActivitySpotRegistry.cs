// Every enabled activity spot, which adventurer holds each (one per spot) and who has reached theirs: reserve a free usable
// one, mark it reached on arrival, release by adventurer.
using System;
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Registry pattern (static) — spots live in spawned room prefabs, so there's no scene object to wire them to.
    // "Reached" (the avatar is sitting / lying there) always implies "held": every way of losing the spot clears both.
    public static class ActivitySpotRegistry
    {
        private static readonly List<ActivitySpot> Spots = new();
        private static readonly Dictionary<ActivitySpot, int> HeldBy = new();
        private static readonly Dictionary<int, ActivitySpot> Reached = new();
        private static readonly List<ActivitySpot> Candidates = new();

        public static IReadOnlyList<ActivitySpot> All => Spots;

        public static void Register(ActivitySpot spot)
        {
            if (spot != null && !Spots.Contains(spot))
                Spots.Add(spot);
        }

        // A vanished spot (room demolished / model swapped) drops its reservation; its avatar re-plans on the room change.
        public static void Unregister(ActivitySpot spot)
        {
            Spots.Remove(spot);
            if (spot == null || !HeldBy.Remove(spot, out var holder))
                return;
            if (Reached.TryGetValue(holder, out var reached) && reached == spot)
                Reached.Remove(holder);
        }

        // Reserves a random free spot of this kind that passes <paramref name="usable"/> (e.g. its cell is walkable).
        public static bool TryReserve(SpotKind kind, int adventurerId, Func<ActivitySpot, bool> usable, out ActivitySpot spot)
        {
            spot = null;
            Candidates.Clear();
            foreach (var candidate in Spots.Where(candidate => candidate != null && candidate.Kind == kind && !HeldBy.ContainsKey(candidate))
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

        // The avatar arrived at its spot (sitting / lying there now).
        // NOTE: Ignored unless they still hold this spot — a stale arrival after a re-plan can't mark anything.
        public static void MarkReached(ActivitySpot spot, int adventurerId)
        {
            if (spot != null && HeldBy.TryGetValue(spot, out var holder) && holder == adventurerId)
                Reached[adventurerId] = spot;
        }

        // The spot this adventurer holds (reserved — they may still be walking there).
        public static bool TryGetHeld(int adventurerId, out ActivitySpot spot)
        {
            spot = (from pair in HeldBy where pair.Value == adventurerId select pair.Key).FirstOrDefault();
            return spot != null;
        }

        // The spot this adventurer has reached and is at right now.
        public static bool TryGetReached(int adventurerId, out ActivitySpot spot) =>
            Reached.TryGetValue(adventurerId, out spot) && spot != null;

        // How many free spots of this kind pass <paramref name="usable"/>.
        public static int CountFree(SpotKind kind, Func<ActivitySpot, bool> usable) =>
            Spots.Count(spot => spot != null && spot.Kind == kind && !HeldBy.ContainsKey(spot) && (usable == null || usable(spot)));

        // Frees whatever spot this adventurer holds (and forgets that they reached it).
        public static void Release(int adventurerId)
        {
            Reached.Remove(adventurerId);
            var held = (from pair in HeldBy where pair.Value == adventurerId select pair.Key).FirstOrDefault();
            if (held != null)
                HeldBy.Remove(held);
        }

        public static int CountOf(SpotKind kind)
        {
            return Spots.Count(spot => spot != null && spot.Kind == kind);
        }

        // Scene teardown: clears reservations and arrivals (spots unregister themselves as they're destroyed).
        public static void ClearReservations()
        {
            HeldBy.Clear();
            Reached.Clear();
        }
    }
}