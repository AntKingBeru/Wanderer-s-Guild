// Reads and validates sheet cells for the importer: text, whole numbers, enums, stat blocks, sizes ("6x4") and lists.
using System;
using System.Linq;
using System.Globalization;
using Csv;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class SheetCells
    {
        public static string Text(CsvRow row, string column) => (row[column].GetValue<string>() ?? string.Empty).Trim();

        // Text where a typed "\n" (backslash + n) and real line breaks (any style) become line breaks.
        public static string MultilineText(CsvRow row, string column) =>
            Text(row, column).Replace("\r\n", "\n").Replace('\r', '\n').Replace("\\n", "\n");

        // Text forced onto one line: any line break (typed "\n" or real) becomes a space.
        public static string SingleLineText(CsvRow row, string column) =>
            MultilineText(row, column).Replace('\n', ' ');

        // False when the column doesn't exist (optional columns); an empty cell returns true with "".
        public static bool TryText(CsvRow row, string column, out string value)
        {
            try
            {
                value = Text(row, column);
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        public static int WholeNumber(CsvRow row, string column) => ParseWholeNumber(Text(row, column), column);

        // By name, ignoring case (e.g. "National", "gathering"). Unknown names throw instead of silently defaulting.
        public static T EnumValue<T>(CsvRow row, string column) where T : struct, Enum => ParseEnum<T>(Text(row, column), column);

        // "Str,Agi,Int,Vit" — exactly four whole numbers (spaces allowed).
        public static StatBlock Stats(CsvRow row, string column)
        {
            var parts = Text(row, column).Split(',');
            if (parts.Length != 4)
                throw new FormatException($"{column} needs 4 numbers (Str,Agi,Int,Vit), got '{Text(row, column)}'");

            return new StatBlock(ParseWholeNumber(parts[0], column), ParseWholeNumber(parts[1], column),
                                 ParseWholeNumber(parts[2], column), ParseWholeNumber(parts[3], column));
        }

        // "6x4" (or "6X4", "6×4") — width × depth, both at least 1.
        public static Vector2Int Size(CsvRow row, string column)
        {
            var text = Text(row, column).Replace('X', 'x').Replace('×', 'x');
            var parts = text.Split('x');
            if (parts.Length != 2)
                throw new FormatException($"{column} needs WxD (e.g. 6x4), got '{text}'");

            Vector2Int size = new(ParseWholeNumber(parts[0], column), ParseWholeNumber(parts[1], column));
            if (size.x < 1 || size.y < 1) throw new FormatException($"{column} '{text}' must be at least 1x1");
            return size;
        }

        // Comma-separated items, trimmed; empty items dropped. An empty cell is an empty list.
        public static string[] List(CsvRow row, string column)
        {
            return Text(row, column).Split(',').Select(part => part.Trim()).Where(item => item.Length > 0).ToArray();
        }

        // Comma-separated whole numbers (e.g. "200,400,800"). Any bad item throws.
        public static int[] WholeNumberList(CsvRow row, string column)
        {
            var items = List(row, column);
            var values = new int[items.Length];
            for (var i = 0; i < items.Length; i++)
                values[i] = ParseWholeNumber(items[i], column);
            return values;
        }

        // Comma-separated enum names (empty cell = none). Unknown names throw.
        public static T[] EnumList<T>(CsvRow row, string column) where T : struct, Enum
        {
            var names = List(row, column);
            var values = new T[names.Length];
            for (var i = 0; i < names.Length; i++)
                values[i] = ParseEnum<T>(names[i], column);
            return values;
        }

        private static int ParseWholeNumber(string text, string column)
        {
            return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new FormatException($"{column} '{text.Trim()}' isn't a whole number");
        }

        private static T ParseEnum<T>(string text, string column) where T : struct, Enum
        {
            if (Enum.TryParse(text.Trim(), true, out T value) && Enum.IsDefined(typeof(T), value))
                return value;
            throw new FormatException($"{column} '{text.Trim()}' isn't a valid {typeof(T).Name}");
        }
    }
}