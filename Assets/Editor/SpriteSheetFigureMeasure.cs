// Editor: measures a sprite sheet's empty space above the head and under the feet (alpha ≥ 0.5), from the source PNG.
using System.IO;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class SpriteSheetFigureMeasure
    {
        // NOTE: Same threshold as the avatar material's alpha clip (0.5) — what's measured is exactly what's drawn.
        private const byte AlphaCutoff = 128;

        // NOTE: Reads the PNG file itself, so the texture needs no Read/Write import flag. The texture is 4 frame-tall bands;
        // scanning each band across the full width covers every frame in it. False for non-PNG, wrong size, or fully transparent.
        public static bool TryMeasure(Texture2D sheetTexture, out int top, out int bottom)
        {
            top = bottom = 0;
            var path = sheetTexture != null ? AssetDatabase.GetAssetPath(sheetTexture) : null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!copy.LoadImage(File.ReadAllBytes(path)) || copy.height % AdventurerSpriteSheet.Rows != 0)
                    return false;

                var frameHeight = copy.height / AdventurerSpriteSheet.Rows;
                var width = copy.width;
                var pixels = copy.GetPixels32();
                var minTop = frameHeight;
                var minBottom = frameHeight;

                for (var y = 0; y < copy.height; y++)
                {
                    var fromBottom = y % frameHeight; // texture rows are bottom-up
                    var fromTop = frameHeight - 1 - fromBottom;
                    if (fromTop >= minTop && fromBottom >= minBottom)
                        continue; // this row can't improve either bound

                    for (var x = 0; x < width; x++)
                    {
                        if (pixels[y * width + x].a < AlphaCutoff)
                            continue;

                        minTop = Mathf.Min(minTop, fromTop);
                        minBottom = Mathf.Min(minBottom, fromBottom);
                        break;
                    }
                }

                if (minTop == frameHeight)
                    return false;

                top = minTop;
                bottom = minBottom;
                return true;
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }
    }
}