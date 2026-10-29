// Draws tiny 12×12 pixel-art placeholders at runtime (coin, person, pause, five reputation faces) until real sprites exist.
using UnityEngine;

namespace WanderersGuild
{
    public static class PlaceholderIcons
    {
        private const int Size = 12;

        private static readonly Color32 Clear = new(0, 0, 0, 0);
        private static readonly Color32 Ink = new(24, 20, 16, 255);
        private static readonly Color32 White = new(255, 255, 255, 255);
        private static readonly Color32 CoinFace = new(232, 200, 72, 255);
        private static readonly Color32 CoinRim = new(160, 116, 40, 255);
        private static readonly Color32 CoinShine = new(255, 240, 168, 255);
        private static readonly Color32 Skin = new(232, 196, 160, 255);
        private static readonly Color32 Tunic = new(88, 128, 184, 255);

        // NOTE: Mouth pixels (x, y from the bottom), worst → best: big frown → big smile.
        private static readonly Vector2Int[][] Mouths =
        {
            new[] { new Vector2Int(2, 2), new(9, 2), new(3, 3), new(8, 3), new(4, 4), new(5, 4), new(6, 4), new(7, 4) },
            new[] { new Vector2Int(3, 3), new(8, 3), new(4, 4), new(5, 4), new(6, 4), new(7, 4) },
            new[] { new Vector2Int(3, 3), new(4, 3), new(5, 3), new(6, 3), new(7, 3), new(8, 3) },
            new[] { new Vector2Int(3, 4), new(8, 4), new(4, 3), new(5, 3), new(6, 3), new(7, 3) },
            new[] { new Vector2Int(2, 4), new(9, 4), new(3, 3), new(8, 3), new(4, 2), new(5, 2), new(6, 2), new(7, 2) }
        };

        private static Texture2D _coin;
        private static Texture2D _person;
        private static Texture2D _pause;
        private static readonly Texture2D[] Faces = new Texture2D[Mouths.Length];

        // NOTE: Runtime textures die when Play stops; with domain reload off the fields survive, so rebuild if destroyed.

        // >A gold coin (not meant to be tinted).
        public static Texture2D Coin => _coin ? _coin : _coin = BuildCoin();

        // >A small adventurer: head and tunic (not meant to be tinted).
        public static Texture2D Person => _person ? _person : _person = BuildPerson();

        // >Two white bars inside a thin frame (the frame tells it apart from the time HUD's speed "||").
        public static Texture2D Pause => _pause ? _pause : _pause = BuildPause();

        // >A white square face (tint it in the level's color); the mouth reflects the level.
        public static Texture2D Face(ReputationLevel level)
        {
            var index = Mathf.Clamp((int)level, 0, Faces.Length - 1);
            if (!Faces[index])
                Faces[index] = BuildFace(Mouths[index]);
            return Faces[index];
        }

        private static Texture2D BuildCoin()
        {
            var pixels = Blank();
            const float centre = (Size - 1) * 0.5f;

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                    if (distance > 5.2f)
                        continue;

                    pixels[y * Size + x] = distance > 4.2f ? CoinRim : CoinFace;
                }
            }

            pixels[7 * Size + 4] = CoinShine;
            pixels[8 * Size + 4] = CoinShine;
            pixels[8 * Size + 5] = CoinShine;
            return ToTexture(pixels, "PlaceholderCoin");
        }

        // NOTE: y counts from the bottom. Head rows 7–10, tunic rows 1–5, an ink outline around both.
        private static Texture2D BuildPerson()
        {
            var pixels = Blank();

            FillRect(pixels, 4, 7, 4, 4, Ink);
            FillRect(pixels, 5, 8, 2, 2, Skin);
            FillRect(pixels, 2, 1, 8, 5, Ink);
            FillRect(pixels, 3, 2, 6, 3, Tunic);
            FillRect(pixels, 5, 5, 2, 1, Skin);   // Neck.
            return ToTexture(pixels, "PlaceholderPerson");
        }

        private static Texture2D BuildPause()
        {
            var pixels = Blank();

            // Frame.
            for (var i = 1; i < Size - 1; i++)
            {
                pixels[1 * Size + i] = White;
                pixels[(Size - 2) * Size + i] = White;
                pixels[i * Size + 1] = White;
                pixels[i * Size + (Size - 2)] = White;
            }

            // Two bars.
            FillRect(pixels, 4, 3, 1, 6, White);
            FillRect(pixels, 7, 3, 1, 6, White);
            return ToTexture(pixels, "PlaceholderPause");
        }

        private static Texture2D BuildFace(Vector2Int[] mouth)
        {
            var pixels = new Color32[Size * Size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = White;

            // Eyes: two 1×2 strokes.
            foreach (var eye in new[] { new Vector2Int(3, 7), new(3, 8), new(8, 7), new(8, 8) })
                pixels[eye.y * Size + eye.x] = Ink;

            foreach (var pixel in mouth)
                pixels[pixel.y * Size + pixel.x] = Ink;

            return ToTexture(pixels, "PlaceholderFace");
        }

        private static void FillRect(Color32[] pixels, int x, int y, int width, int height, Color32 color)
        {
            for (var row = y; row < y + height && row < Size; row++)
            {
                for (var column = x; column < x + width && column < Size; column++)
                    pixels[row * Size + column] = color;
            }
        }

        private static Color32[] Blank()
        {
            var pixels = new Color32[Size * Size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Clear;
            return pixels;
        }

        private static Texture2D ToTexture(Color32[] pixels, string name)
        {
            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }

        // NOTE: With domain reload disabled, clear references to last session's (destroyed) textures.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _coin = null;
            _person = null;
            _pause = null;
            System.Array.Clear(Faces, 0, Faces.Length);
        }
    }
}