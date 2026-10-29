// One entry in the save-slot list: thumbnail, guild name + rank, in-game date + play time, and real save time.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class SaveSlotRow
    {
        public const string UssClassName = "save-slot";
        public const string SelectedUssClassName = UssClassName + "--selected";
        public const string UnreadableUssClassName = UssClassName + "--unreadable";
        public const string ThumbnailUssClassName = UssClassName + "__thumbnail";
        public const string DetailsUssClassName = UssClassName + "__details";
        public const string LineUssClassName = UssClassName + "__line";
        public const string NameUssClassName = UssClassName + "__name";
        public const string RankUssClassName = UssClassName + "__rank";
        public const string ProgressUssClassName = UssClassName + "__progress";
        public const string SavedAtUssClassName = UssClassName + "__saved-at";
        public const string StatusUssClassName = UssClassName + "__status";

        private readonly VisualElement _thumbnail;
        private readonly VisualElement _details;
        private readonly OutlinedText _name;
        private readonly OutlinedText _rank;
        private readonly OutlinedText _progress;
        private readonly OutlinedText _savedAt;
        private readonly OutlinedText _status;

        public int Index { get; }
        public Button Button { get; }

        public SaveSlotRow(int index)
        {
            Index = index;
            Button = new Button();
            Button.AddToClassList("pixel-button");
            Button.AddToClassList(UssClassName);

            _thumbnail = AddPart(Button, ThumbnailUssClassName);
            _details = AddPart(Button, DetailsUssClassName);

            var top = AddPart(_details, LineUssClassName);
            _name = AddText(top, NameUssClassName);
            _rank = AddText(top, RankUssClassName);

            var bottom = AddPart(_details, LineUssClassName);
            _progress = AddText(bottom, ProgressUssClassName);
            _savedAt = AddText(bottom, SavedAtUssClassName);

            // NOTE: Shown instead of the details for empty or unreadable slots.
            _status = AddText(Button, StatusUssClassName);
        }

        /// <param name="info"></param>
        /// <param name="thumbnail">Owned by SaveSlotManager's cache; the row only displays it.</param>
        /// <param name="savedAtFormat"></param>
        public void Bind(SaveSlotInfo info, Texture2D thumbnail, string savedAtFormat)
        {
            var ready = info is { State: SaveSlotState.Ready, Header: not null };

            // NOTE: Unreadable rows stay selectable (so they can be deleted); this class only grays their text.
            Button.EnableInClassList(UnreadableUssClassName, info.State == SaveSlotState.Corrupted);

            _details.style.display = ready ? DisplayStyle.Flex : DisplayStyle.None;
            _status.style.display = ready ? DisplayStyle.None : DisplayStyle.Flex;
            _thumbnail.style.backgroundImage = ready && thumbnail
                ? new StyleBackground(thumbnail)
                : new StyleBackground(StyleKeyword.None);

            if (!ready)
            {
                _status.Text = info.State == SaveSlotState.Corrupted ? "Unreadable Save" : "Empty Slot";
                return;
            }

            var header = info.Header;
            _name.Text = header.GuildName;
            _rank.Text = SaveSlotFormatter.Rank(header.Rank);
            _progress.Text = $"{SaveSlotFormatter.Date(header.Date)}  {SaveSlotFormatter.PlayTime(header.PlayTimeSeconds)}";
            _savedAt.Text = SaveSlotFormatter.SavedAt(header.SavedAtLocal, savedAtFormat);
        }

        /// <summary>Marks the slot Delete acts on, even while focus is on the footer.</summary>
        public void SetSelected(bool selected) => Button.EnableInClassList(SelectedUssClassName, selected);

        private static VisualElement AddPart(VisualElement parent, string ussClass)
        {
            VisualElement part = new() { pickingMode = PickingMode.Ignore };
            part.AddToClassList(ussClass);
            parent.Add(part);
            return part;
        }

        private static OutlinedText AddText(VisualElement parent, string ussClass)
        {
            OutlinedText text = new();
            text.AddToClassList(ussClass);
            parent.Add(text);
            return text;
        }
    }
}