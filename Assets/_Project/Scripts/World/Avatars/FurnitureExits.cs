// Where an avatar gets up from furniture: the free cell it approached the spot from (if still valid), else the nearest free cell.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Remembers one approach cell per adventurer (set when they head to a spot) — getting up on the side they came from.
    public sealed class FurnitureExits
    {
        private const int MaxReach = 2; // NOTE: Cells (Chebyshev); a remembered cell farther away is stale (moved since).

        private readonly Dictionary<int, Vector2Int> _approachedFrom = new();

        public void Remember(int adventurerId, Vector2Int approach) => _approachedFrom[adventurerId] = approach;

        public void Forget(int adventurerId) => _approachedFrom.Remove(adventurerId);

        // NOTE: The remembered cell when it's still free, in the same room and close; otherwise the nearest free cell in the
        // room, preferring that side. False if the room has no free cell.
        public bool TryResolve(GuildNavigator navigator, int adventurerId, Vector2Int from, out Vector2Int exit)
        {
            var hasRemembered = _approachedFrom.TryGetValue(adventurerId, out var remembered);
            if (hasRemembered && navigator.IsFree(remembered) && navigator.RoomOf(remembered) == navigator.RoomOf(from) &&
                Mathf.Max(Mathf.Abs(remembered.x - from.x), Mathf.Abs(remembered.y - from.y)) <= MaxReach)
            {
                exit = remembered;
                return true;
            }

            return navigator.TryNearestFree(from, hasRemembered ? remembered : from, out exit);
        }
    }
}