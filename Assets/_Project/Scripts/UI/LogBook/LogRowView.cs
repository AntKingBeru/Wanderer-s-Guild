// One row of the log book list: a time column and a one-line text; a date header shows its text alone, in accent.
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Rows are recycled by the ListView — Bind resets every class each time.
    public static class LogRowView
    {
        private const string RowClass = "log-row";
        private const string HeaderClass = RowClass + "--header";
        private const string GoodClass = RowClass + "--good";
        private const string BadClass = RowClass + "--bad";

        public static VisualElement Create()
        {
            VisualElement row = new() { pickingMode = PickingMode.Ignore };
            row.AddToClassList(RowClass);

            OutlinedText time = new();
            time.AddToClassList(RowClass + "__time");
            OutlinedText text = new();
            text.AddToClassList(RowClass + "__text");

            row.Add(time); // NOTE: Child 0 = time, child 1 = text (Bind reads them by index).
            row.Add(text);
            return row;
        }

        public static void Bind(VisualElement row, LogLine line)
        {
            ((OutlinedText)row[0]).Text = line.Time;
            ((OutlinedText)row[1]).Text = line.Text;

            row.EnableInClassList(HeaderClass, line.IsHeader);
            row.EnableInClassList(GoodClass, line is { IsHeader: false, Tone: LogTone.Good });
            row.EnableInClassList(BadClass, line is { IsHeader: false, Tone: LogTone.Bad });
        }
    }
}