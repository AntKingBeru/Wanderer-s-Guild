// An avatar sprite sheet: 4 rows (Down, Left, Right, Up) × columns (frame 0 = idle, then walk frames) of fixed-size frames.
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

        public Texture2D Texture => texture;
        public float WalkFramesPerSecond => walkFramesPerSecond;

        // Frames per row, from the texture width and the frame width.
        public int ColumnsFor(Vector2Int frameSize) => texture ? Mathf.Max(1, texture.width / frameSize.x) : 1;

        // True if the texture is a whole number of frames wide and exactly 4 frames tall.
        public bool IsValid(Vector2Int frameSize, out string problem)
        {
            problem = null;
            if (texture is null)
                problem = "has no texture";
            else if (texture.width % frameSize.x != 0 || texture.width < frameSize.x)
                problem = $"is {texture.width}px wide — not a whole number of {frameSize.x}px frames";
            else if (texture.height != frameSize.y * Rows)
                problem = $"is {texture.height}px tall — needs {frameSize.y * Rows} (4 rows of {frameSize.y}px)";
            return problem == null;
        }

        // Runtime-only sheet (placeholders).
        public static AdventurerSpriteSheet CreateRuntime(Texture2D texture)
        {
            var sheet = CreateInstance<AdventurerSpriteSheet>();
            sheet.texture = texture;
            sheet.hideFlags = HideFlags.DontSave;
            return sheet;
        }
    }
}