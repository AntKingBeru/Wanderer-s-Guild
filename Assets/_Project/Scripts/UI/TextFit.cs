// Predicts how the pixel font wraps text in a box N characters wide (monospace): lines used and longest line, for text limits.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Mirrors TextCore's greedy word wrap for a monospace font: words break at spaces, a space at a line's end costs
    // nothing, "\n" starts a new line, and a word longer than a whole line is cut by characters. Rich-text tags aren't
    // stripped (the data has none).
    public static class TextFit
    {
        // Characters that fit on one line of a text element this wide (its 2px + 2px gutter included).
        // NOTE: Boxes keep at least 0.5px slack over a whole character, so an exact fit never meets float rounding.
        public static int CharsPerLine(float elementWidth) =>
            Mathf.Max(1, Mathf.FloorToInt((elementWidth - TextLimits.Gutter) / TextLimits.CharAdvance));

        // Lines the text takes wrapped at this many characters (0 for empty text).
        public static int CountLines(string text, int charsPerLine)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var lines = 0;
            foreach (var paragraph in Paragraphs(text))
                lines += ParagraphLines(paragraph, Mathf.Max(1, charsPerLine));
            return lines;
        }

        // Length of the longest explicit line (what an unwrapped one-line box must hold); trailing spaces don't count.
        public static int LongestLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var longest = 0;
            foreach (var paragraph in Paragraphs(text))
                longest = Mathf.Max(longest, paragraph.TrimEnd().Length);
            return longest;
        }

        private static string[] Paragraphs(string text) => text.Replace("\r", string.Empty).Split('\n');

        // NOTE: used = characters already on the current line (0 = empty line, so the next word needs no space before it).
        private static int ParagraphLines(string paragraph, int max)
        {
            var lines = 1;
            var used = 0;
            foreach (var word in paragraph.Split(' '))
            {
                var length = word.Length;
                var needed = used == 0 ? length : used + 1 + length;
                if (needed <= max)
                {
                    used = needed;
                    continue;
                }

                if (used > 0)
                    lines++;

                // A word longer than a whole line fills lines by characters; its remainder starts the next one.
                while (length > max)
                {
                    lines++;
                    length -= max;
                }
                used = length;
            }
            return lines;
        }
    }
}