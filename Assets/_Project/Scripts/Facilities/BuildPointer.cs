// Turns the pointer into a point on the guild floor and a grid cell (through the 320×180 world view).
using UnityEngine;

namespace WanderersGuild
{
    public static class BuildPointer
    {
        // The floor point under the pointer, and its view pixel. False outside the view or when the ray misses the floor.
        public static bool TryFloorPoint(Camera camera, WorldViewPresenter view, Vector2 screen, float floorHeight,
            out Vector3 point, out Vector2 viewPixel)
        {
            point = default;
            if (!view.TryScreenToView(screen, out viewPixel))
                return false;

            var size = GameConfig.Instance.WorldView.RenderSize;
            var ray = camera.ViewportPointToRay(new Vector3(viewPixel.x / size.x, 1f - viewPixel.y / size.y, 0f));
            Plane floor = new(Vector3.up, new Vector3(0f, floorHeight, 0f));

            if (!floor.Raycast(ray, out var distance))
                return false;
            point = ray.GetPoint(distance);
            return true;
        }

        // The grid cell containing a world point (Grid with Cell Swizzle XZY: cell y = world z).
        public static Vector2Int CellAt(Grid grid, Vector3 point)
        {
            var cell = grid.WorldToCell(point);
            return new Vector2Int(cell.x, cell.y);
        }
    }
}