// Footprint maths for rotated rooms: rotated size and cells, rotated 2-wide doors, and the prefab's world pivot.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class FacilityFootprint
    {
        // Every door is this many cells wide.
        public const int DoorWidth = 2;

        // Size after rotation (0–3): 90° and 270° swap width and depth.
        public static Vector2Int RotatedSize(Vector2Int size, int rotation) =>
            (rotation & 1) == 0 ? size : new Vector2Int(size.y, size.x);

        // A local cell of the unrotated footprint → the world cell for this origin and rotation.
        // NOTE: Rotating by cell centres, then shifting the footprint back to positive cells.
        public static Vector2Int ToWorldCell(Vector2Int local, Vector2Int size, Vector2Int origin, int rotation)
        {
            int w = size.x, d = size.y;
            var rotated = (rotation & 3) switch
            {
                1 => new Vector2Int(local.y, w - 1 - local.x),
                2 => new Vector2Int(w - 1 - local.x, d - 1 - local.y),
                3 => new Vector2Int(d - 1 - local.y, local.x),
                _ => local
            };
            return origin + rotated;
        }

        // A door of the unrotated footprint → its world door (both cells rotated, facing rotated).
        public static WorldDoor ToWorldDoor(ConnectorSpec door, Vector2Int size, Vector2Int origin, int rotation)
        {
            var second = door.Cell + EdgeStep(door.Facing);
            return new WorldDoor(ToWorldCell(door.Cell, size, origin, rotation), ToWorldCell(second, size, origin, rotation),
                                 Rotate(door.Facing, rotation));
        }

        // Step along a door's edge: north/south doors run along x, east/west doors along z.
        public static Vector2Int EdgeStep(CardinalDirection facing) =>
            facing is CardinalDirection.North or CardinalDirection.South ? new Vector2Int(1, 0) : new Vector2Int(0, 1);

        public static CardinalDirection Rotate(CardinalDirection facing, int rotation) => (CardinalDirection)(((int)facing + rotation) & 3);

        public static CardinalDirection Opposite(CardinalDirection facing) => (CardinalDirection)(((int)facing + 2) & 3);

        public static Vector2Int Step(CardinalDirection facing) => facing switch
        {
            CardinalDirection.North => new Vector2Int(0, 1),
            CardinalDirection.East => new Vector2Int(1, 0),
            CardinalDirection.South => new Vector2Int(0, -1),
            _ => new Vector2Int(-1, 0)
        };

        public static Vector3 ToVector(CardinalDirection facing)
        {
            var step = Step(facing);
            return new Vector3(step.x, 0f, step.y);
        }

        public static IEnumerable<Vector2Int> Cells(Vector2Int origin, Vector2Int size, int rotation)
        {
            var rotated = RotatedSize(size, rotation);
            for (var x = 0; x < rotated.x; x++)
            {
                for (var y = 0; y < rotated.y; y++)
                    yield return new Vector2Int(origin.x + x, origin.y + y);
            }
        }

        // Pivot cell (the prefab's corner after rotation): offset by (0,W) / (W,D) / (D,0) for 90/180/270°.
        public static Vector2Int PivotCell(Vector2Int origin, Vector2Int size, int rotation) => origin + ((rotation & 3) switch
        {
            1 => new Vector2Int(0, size.x),
            2 => new Vector2Int(size.x, size.y),
            3 => new Vector2Int(size.y, 0),
            _ => Vector2Int.zero
        });

        public static Quaternion RotationOf(int rotation) => Quaternion.Euler(0f, 90f * (rotation & 3), 0f);
    }
}