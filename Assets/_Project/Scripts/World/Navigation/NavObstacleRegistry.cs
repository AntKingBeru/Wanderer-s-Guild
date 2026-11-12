// Every enabled nav obstacle, plus a version that changes whenever one appears or disappears (the navigator rebuilds then).
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Registry pattern (static) — obstacles live in spawned room prefabs, like activity spots.
    public static class NavObstacleRegistry
    {
        private static readonly List<NavObstacle> Obstacles = new();

        public static IReadOnlyList<NavObstacle> All => Obstacles;

        // NOTE: Bumped on every add/remove; compared each frame by the navigator.
        public static int Version { get; private set; }

        public static void Register(NavObstacle obstacle)
        {
            if (obstacle == null || Obstacles.Contains(obstacle))
                return;
            Obstacles.Add(obstacle);
            Version++;
        }

        public static void Unregister(NavObstacle obstacle)
        {
            if (Obstacles.Remove(obstacle))
                Version++;
        }
    }
}