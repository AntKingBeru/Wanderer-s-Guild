// Runtime 12×12 placeholder season icons (a leaf in the season's color) until GuildHudArt has real ones.
using UnityEngine;

namespace WanderersGuild
{
    public static class SeasonPlaceholderIcons
    {
        private const int Size = 12;

        private static readonly Color[] Colors =
        {
            new(0.47f, 0.78f, 0.38f), // Spring
            new(0.95f, 0.80f, 0.25f), // Summer
            new(0.85f, 0.45f, 0.20f), // Autumn
            new(0.70f, 0.85f, 0.95f)  // Winter
        };

        private static readonly Texture2D[] Cache = new Texture2D[4];

        // NOTE: 0-based season (month − 1, wrapped). A diamond "leaf" with a dark outline, point-filtered.
        public static Texture2D For(int season)
        {
            var index = (season % 4 + 4) % 4;
            if (Cache[index] is not null)
                return Cache[index];

            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color clear = new(0f, 0f, 0f, 0f), outline = new(0.1f, 0.08f, 0.06f), fill = Colors[index];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var d = Mathf.Abs(x - 5) + Mathf.Abs(y - 5);
                    texture.SetPixel(x, y, d < 5 ? fill : d == 5 ? outline : clear);
                }
            }
            texture.Apply();
            return Cache[index] = texture;
        }
    }
}