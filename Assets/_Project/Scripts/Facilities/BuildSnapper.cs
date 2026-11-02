// Finds the nearest fitting connection for the build ghost (an open door it can attach to, flush and free) within range.
using UnityEngine;

namespace WanderersGuild
{
    public static class BuildSnapper
    {
        // The snapped origin/rotation, or the free one if nothing fits within range. The player's rotation is
        // preferred; other rotations cost a small penalty.
        public static (Vector2Int origin, int rotation) Snap(FacilityData data, Vector2Int freeOrigin, int rotation,
                                                             FacilityManager manager, BuildModeSettings build, FacilitySettings settings)
        {
            var freeCentre = Centre(freeOrigin, data.Size, rotation);
            (Vector2Int origin, int rotation) best = (freeOrigin, rotation);
            var bestScore = float.MaxValue;

            foreach (var room in manager.Rooms)
            {
                foreach (var door in room.Doors)
                {
                    if (!manager.Grid.IsOpen(door))
                        continue;

                    var target = door.Partner;
                    for (var r = 0; r < 4; r++)
                    {
                        foreach (var connector in data.Connectors)
                        {
                            var local = FacilityFootprint.ToWorldDoor(connector, data.Size, Vector2Int.zero, r);
                            if (local.Facing != target.Facing)
                                continue;

                            var origin = target.A - local.A;
                            var distance = Vector2.Distance(Centre(origin, data.Size, r), freeCentre);
                            if (distance > build.SnapDistance)
                                continue;

                            var score = distance + (r == rotation ? 0f : build.RotationSnapPenalty);
                            if (score >= bestScore)
                                continue;
                            if (FacilityRules.GeometryReason(data, origin, r, manager.Grid, settings) != null)
                                continue;

                            bestScore = score;
                            best = (origin, r);
                        }
                    }
                }
            }

            return best;
        }

        private static Vector2 Centre(Vector2Int origin, Vector2Int size, int rotation)
        {
            var rotated = FacilityFootprint.RotatedSize(size, rotation);
            return new Vector2(origin.x + rotated.x * 0.5f, origin.y + rotated.y * 0.5f);
        }
    }
}