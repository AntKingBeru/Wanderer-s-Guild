// One pooled roster line: the mood face (outside the row's box), then the row — rank badge, given name (colored when ready
// for a promotion or class advancement), class icon + class, and the level (or the status for anyone not idle).
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Root (the line) goes into the list; Button stays the row itself — selection, clicks and navigation use it only.
    // Every row has the same parts at the same widths; only the name column flexes.
    public sealed class RosterRowView
    {
        private const string SelectedClass = "roster-row--selected";
        private const string StatusClass = "roster-row__level--status";
        private const string PromotionClass = "roster-row__name--promotion";
        private const string AdvancementClass = "roster-row__name--advancement";

        private readonly VisualElement _badge;
        private readonly OutlinedText _badgeLabel;
        private readonly OutlinedText _name;
        private readonly IconSlot _mood;
        private readonly IconSlot _classIcon;
        private readonly OutlinedText _class;
        private readonly OutlinedText _level;

        // The whole line (mood face + row) — what the list holds.
        public VisualElement Root { get; }
        public Button Button { get; }
        public Adventurer Adventurer { get; private set; }

        public RosterRowView(Action<RosterRowView> onClicked)
        {
            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            Root.AddToClassList("roster-line");
            _mood = IconSlot.Create("roster-line__mood");

            Button = new Button(() => onClicked(this));
            Button.AddToClassList("pixel-button");
            Button.AddToClassList("roster-row");

            _badge = new VisualElement { pickingMode = PickingMode.Ignore };
            _badge.AddToClassList("roster-row__badge");
            _badgeLabel = new OutlinedText();
            _badge.Add(_badgeLabel);

            _name = Part("roster-row__name");
            _classIcon = IconSlot.Create("roster-row__class-icon");
            _class = Part("roster-row__class");
            _level = Part("roster-row__level");

            Button.Add(_badge);
            Button.Add(_name);
            Button.Add(_classIcon.Root);
            Button.Add(_class);
            Button.Add(_level);

            Root.Add(_mood.Root);
            Root.Add(Button);

            Hide();
        }

        // NOTE: Only the given name fits the row; the full name shows in the details pane. Every part (and both readiness
        // colors) is set on each Show, so a pooled line never keeps the last adventurer's color, mood or status.
        public void Show(Adventurer adventurer, Color rankColor, bool isSelected, RosterReadiness readiness, SatisfactionLevel? mood)
        {
            Adventurer = adventurer;

            _badge.style.backgroundColor = rankColor;
            _badgeLabel.Text = RankFormatter.BadgeLabel(adventurer.Rank);
            _name.Text = AdventurerFormatter.GivenName(adventurer.Name);
            _name.EnableInClassList(PromotionClass, readiness == RosterReadiness.Promotion);
            _name.EnableInClassList(AdvancementClass, readiness == RosterReadiness.Advancement);
            ShowMood(mood);
            _classIcon.Set(adventurer.Class.IconFor(adventurer.Rank), adventurer.Class.DisplayName);
            _class.Text = adventurer.Class.DisplayName;

            var status = AdventurerFormatter.RowStatus(adventurer);
            _level.Text = status ?? AdventurerFormatter.ShortLevel(adventurer);
            _level.EnableInClassList(StatusClass, status != null);

            Button.EnableInClassList(SelectedClass, isSelected);
            Root.style.display = DisplayStyle.Flex;
            Button.SetEnabled(true);
        }

        // NOTE: The whole line leaves the layout (no empty face slots); the button is also disabled — display:none alone
        // would keep it reachable by keyboard navigation.
        public void Hide()
        {
            Adventurer = null;
            Root.style.display = DisplayStyle.None;
            Button.SetEnabled(false);
        }

        // NOTE: GameConfig › Satisfaction › Mood Icons; without a sprite, the level's letter in its color. No system → hidden
        // (every line alike, so the boxes still line up).
        private void ShowMood(SatisfactionLevel? mood)
        {
            if (mood == null)
            {
                _mood.Hide();
                return;
            }

            var settings = GameConfig.Instance.Satisfaction;
            var level = mood.Value;
            _mood.Set(settings.IconFor(level), MoodFormatter.LevelName(level));
            _mood.SetLetterColor(settings.ColorOf(level));
        }

        private static OutlinedText Part(string className)
        {
            OutlinedText text = new();
            text.AddToClassList(className);
            return text;
        }
    }
}