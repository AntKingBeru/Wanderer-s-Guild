// A tutorial target on the floor: where a room will go (its footprint, up to about wall height), framed on screen.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Without a grid it can't be framed, but stays "available" — the placement step still works, just without the frame.
    public sealed class TutorialFootprintTarget : ITutorialTarget
    {
        private readonly TutorialPlacement _placement;
        private readonly Grid _grid;
        private readonly TutorialWorldProjector _projector;

        public TutorialFootprintTarget(TutorialPlacement placement, Grid grid, TutorialWorldProjector projector)
        {
            _placement = placement;
            _grid = grid;
            _projector = projector;
        }

        public bool IsAvailable => _placement.Facility != null;

        public VisualElement Element => null;

        public bool TryGetRect(out Rect panelRect)
        {
            panelRect = default;
            return IsAvailable && _grid != null && _projector != null && _projector.TryProject(Bounds(), out panelRect);
        }

        // NOTE: The ghost already marks the spot.
        public void SetEmphasis(bool on) { }

        // NOTE: From the origin cell's corner to past the far cell (grid y = world z, Cell Swizzle XZY), floor to the set height.
        private Bounds Bounds()
        {
            var size = FacilityFootprint.RotatedSize(_placement.Facility.Size, _placement.Rotation);
            var floor = GameConfig.Instance.BuildMode.FloorHeight;
            var top = floor + GameConfig.Instance.Tutorial.FootprintHeight;
            var a = _grid.CellToWorld(new Vector3Int(_placement.Origin.x, _placement.Origin.y, 0));
            var b = _grid.CellToWorld(new Vector3Int(_placement.Origin.x + size.x, _placement.Origin.y + size.y, 0));

            Bounds bounds = new();
            bounds.SetMinMax(new Vector3(Mathf.Min(a.x, b.x), floor, Mathf.Min(a.z, b.z)),
                             new Vector3(Mathf.Max(a.x, b.x), top, Mathf.Max(a.z, b.z)));
            return bounds;
        }
    }
}