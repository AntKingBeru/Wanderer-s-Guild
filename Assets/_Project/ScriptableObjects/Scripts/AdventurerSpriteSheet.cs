// An avatar sprite sheet: 4 rows (Down, Left, Right, Up) × columns (0 = idle, then walk frames), plus the measured figure bounds
// (empty pixels above the head / under the feet) that let poses fit short and tall species.
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Adventurer Sprite Sheet", fileName = "AdventurerSpriteSheet")]
    public class AdventurerSpriteSheet : ScriptableObject
    {
        public const int Rows = 4;

        [Tooltip("Rows top → bottom: Down (facing the camera), Left, Right, Up (away). Frames are GameConfig › Avatars › Frame " +
                 "Size (48×52); columns = texture width ÷ frame width (column 0 = idle, the rest = walk cycle). Import: Point, " +
                 "no compression, Generate Mip Maps on + Mip Maps Preserve Coverage (alpha 0.5), Wrap Clamp.")]
        [SerializeField] private Texture2D texture;

        [SerializeField, Min(1f)] private float walkFramesPerSecond = 6f;

        [Header("Figure (measured automatically — Tools › Wanderer's Guild › Avatars › Measure Sprite Sheets)")]
        [Tooltip("Empty sheet pixels above the tallest point of the figure, over every frame.")]
        [SerializeField, Min(0)] private int figureTop;
        [Tooltip("Empty sheet pixels under the feet, over every frame.")]
        [SerializeField, Min(0)] private int figureBottom;

        public Texture2D Texture => texture;
        public float WalkFramesPerSecond => walkFramesPerSecond;

        // NOTE: 0 / 0 (unmeasured, or a placeholder) means the figure fills the whole frame — the old behavior.
        public int FigureTop => figureTop;
        public int FigureBottom => figureBottom;

        // Frames per row, from the texture width and the frame width.
        public int ColumnsFor(Vector2Int frameSize) => texture != null ? Mathf.Max(1, texture.width / frameSize.x) : 1;

        // True if the texture is a whole number of frames wide and exactly 4 frames tall.
        public bool IsValid(Vector2Int frameSize, out string problem)
        {
            problem = null;
            if (texture == null)
                problem = "has no texture";
            else if (texture.width % frameSize.x != 0 || texture.width < frameSize.x)
                problem = $"is {texture.width}px wide — not a whole number of {frameSize.x}px frames";
            else if (texture.height != frameSize.y * Rows)
                problem = $"is {texture.height}px tall — needs {frameSize.y * Rows} (4 rows of {frameSize.y}px)";
            return problem == null;
        }

        // Runtime-only sheet (placeholders — their figure fills the frame).
        public static AdventurerSpriteSheet CreateRuntime(Texture2D texture)
        {
            var sheet = CreateInstance<AdventurerSpriteSheet>();
            sheet.texture = texture;
            sheet.hideFlags = HideFlags.DontSave;
            return sheet;
        }

#if UNITY_EDITOR
        // Editor-only: set by the measuring tool. True if it changed (so unchanged sheets are never dirtied).
        public bool SetFigure(int top, int bottom)
        {
            top = Mathf.Max(0, top);
            bottom = Mathf.Max(0, bottom);
            if (top == figureTop && bottom == figureBottom)
                return false;

            figureTop = top;
            figureBottom = bottom;
            return true;
        }
#endif
    }
}