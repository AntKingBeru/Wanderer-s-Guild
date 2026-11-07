// Generates simple pixel-person sheets at the configured frame size, colored per species (skin) + class (clothes).
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class PlaceholderAvatarSheets
    {
        // NOTE: Drawn on a small 12×13 design grid, then scaled up by whole pixels to the frame size (48×52 = ×4).
        private const int DesignWidth = 12;
        private const int DesignHeight = 13;
        private const int Columns = 3;

        private static readonly Color32 Empty = new(0, 0, 0, 0);
        private static readonly Color32 Ink = new(24, 20, 28, 255);
        private static readonly Color32 Legs = new(70, 60, 80, 255);
        private static readonly Dictionary<string, AdventurerSpriteSheet> Cache = new();

        // A cached placeholder sheet for this species + class (frame size from the settings).
        public static AdventurerSpriteSheet For(SpeciesData species, ClassData classData, Vector2Int frameSize)
        {
            var key = $"{(species ? species.Id : "")}|{(classData ? classData.Id : "")}|{frameSize.x}x{frameSize.y}";
            if (Cache.TryGetValue(key, out var cached) && cached)
                return cached;

            var texture = Build(Hue(classData ? classData.Id : "", 0.55f, 0.85f),
                                      Hue(species ? species.Id : "", 0.35f, 0.92f), frameSize);
            var sheet = AdventurerSpriteSheet.CreateRuntime(texture);
            Cache[key] = sheet;
            return sheet;
        }

        // Destroys every generated texture and sheet (scene teardown).
        public static void Clear()
        {
            foreach (var sheet in Cache.Values.Where(sheet => sheet))
            {
                if (sheet.Texture)
                    Object.Destroy(sheet.Texture);
                Object.Destroy(sheet);
            }

            Cache.Clear();
        }

        private static Color32 Hue(string key, float saturation, float value) =>
            Color.HSVToRGB((key.GetHashCode() & 0xFFFF) / 65535f, saturation, value);

        // NOTE: Texture rows are bottom-up; sheet row r (0 = Down, top) lives at y = (3 − r) × frame height. Mipmaps are
        // generated (nearest, no alpha averaging) so the 3–5× on-screen shrink stays stable.
        private static Texture2D Build(Color32 body, Color32 skin, Vector2Int frame)
        {
            int width = frame.x * Columns, height = frame.y * AdventurerSpriteSheet.Rows;
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Empty;

            var design = new Color32[DesignWidth * DesignHeight];
            for (var row = 0; row < AdventurerSpriteSheet.Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    for (var i = 0; i < design.Length; i++)
                        design[i] = Empty;
                    DrawPerson(design, (AvatarFacing)row, column, body, skin);
                    Blit(design, pixels, width, column * frame.x, (AdventurerSpriteSheet.Rows - 1 - row) * frame.y, frame);
                }
            }

            Texture2D texture = new(width, height, TextureFormat.RGBA32, true)
            {
                name = "PlaceholderAvatar",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels, 0);
            WriteNearestMips(texture, pixels, width, height);
            texture.Apply(false);
            return texture;
        }

        // NOTE: Each mip keeps hard alpha (nearest pick), so alpha clipping keeps the silhouette at small sizes.
        private static void WriteNearestMips(Texture2D texture, Color32[] source, int width, int height)
        {
            for (var mip = 1; mip < texture.mipmapCount; mip++)
            {
                int w = Mathf.Max(1, width >> mip), h = Mathf.Max(1, height >> mip);
                var level = new Color32[w * h];
                for (var y = 0; y < h; y++)
                    for (var x = 0; x < w; x++)
                        level[y * w + x] = source[(y * height / h) * width + (x * width / w)];
                texture.SetPixels32(level, mip);
            }
        }

        // NOTE: Whole-pixel scale of the design into the frame, bottom-centred.
        private static void Blit(Color32[] design, Color32[] pixels, int width, int x0, int y0, Vector2Int frame)
        {
            var scale = Mathf.Max(1, Mathf.Min(frame.x / DesignWidth, frame.y / DesignHeight));
            var offsetX = (frame.x - DesignWidth * scale) / 2;
            for (var dy = 0; dy < DesignHeight; dy++)
            {
                for (var dx = 0; dx < DesignWidth; dx++)
                {
                    var color = design[dy * DesignWidth + dx];
                    if (color.a == 0)
                        continue;

                    for (var sy = 0; sy < scale; sy++)
                        for (var sx = 0; sx < scale; sx++)
                            Set(pixels, width, x0 + offsetX + dx * scale + sx, y0 + dy * scale + sy, color);
                }
            }
        }

        // NOTE: Design grid, y from the bottom: legs 0–3, body 4–8, head 9–12. Frame 0 = feet together; 1/2 = stride.
        private static void DrawPerson(Color32[] d, AvatarFacing facing, int frame, Color32 body, Color32 skin)
        {
            int leftLeg = frame == 1 ? 1 : 0, rightLeg = frame == 2 ? 1 : 0;
            Fill(d, 4, leftLeg, 2, 4 - leftLeg, Legs);
            Fill(d, 6, rightLeg, 2, 4 - rightLeg, Legs);

            Fill(d, 2, 4, 8, 5, Ink);
            Fill(d, 3, 5, 6, 3, body);

            Fill(d, 3, 9, 6, 4, Ink);
            Fill(d, 4, 9, 4, 3, skin);

            switch (facing)
            {
                case AvatarFacing.Down:
                    Set(d, DesignWidth, 5, 10, Ink);
                    Set(d, DesignWidth, 6, 10, Ink);
                    break;
                case AvatarFacing.Left:
                    Set(d, DesignWidth, 4, 10, Ink);
                    break;
                case AvatarFacing.Right:
                    Set(d, DesignWidth, 7, 10, Ink);
                    break;
                case AvatarFacing.Up:
                    Fill(d, 4, 9, 4, 3, Legs); // Back of the head (hair).
                    break;
            }
        }

        private static void Fill(Color32[] d, int x, int y, int w, int h, Color32 color)
        {
            for (var yy = y; yy < y + h; yy++)
                for (var xx = x; xx < x + w; xx++)
                    Set(d, DesignWidth, xx, yy, color);
        }

        private static void Set(Color32[] pixels, int width, int x, int y, Color32 color)
        {
            var index = y * width + x;
            if (x >= 0 && x < width && index >= 0 && index < pixels.Length)
                pixels[index] = color;
        }
    }
}