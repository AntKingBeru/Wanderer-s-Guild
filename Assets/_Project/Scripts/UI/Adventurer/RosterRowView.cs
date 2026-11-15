// One pooled roster row: rank badge, given name, a ready flag (promotion / advancement), class icon + class, and the level —
// or the status for anyone not idle. Hidden rows are disabled so keyboard navigation skips them.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RosterRowView
    {
        private const string SelectedClass = "roster-row--selected";
        private const string StatusClass = "roster-row__level--status";

        private readonly VisualElement _badge;
        private readonly OutlinedText _badgeLabel;
        private readonly OutlinedText _name;
        private readonly IconSlot _ready;
        private readonly IconSlot _classIcon;
        private readonly OutlinedText _class;
        private readonly OutlinedText _level;

        public Button Button { get; }
        public Adventurer Adventurer { get; private set; }

        public RosterRowView(Action<RosterRowView> onClicked)
        {
            Button = new Button(() => onClicked(this));
            Button.AddToClassList("pixel-button");
            Button.AddToClassList("roster-row");

            _badge = new VisualElement { pickingMode = PickingMode.Ignore };
            _badge.AddToClassList("roster-row__badge");
            _badgeLabel = new OutlinedText();
            _badge.Add(_badgeLabel);

            _name = Part("roster-row__name");
            _ready = IconSlot.Create("roster-row__ready");
            _classIcon = IconSlot.Create("roster-row__class-icon");
            _class = Part("roster-row__class");
            _level = Part("roster-row__level");

            Button.Add(_badge);
            Button.Add(_name);
            Button.Add(_ready.Root);
            Button.Add(_classIcon.Root);
            Button.Add(_class);
            Button.Add(_level);

            Hide();
        }

        // NOTE: Only the given name fits the row; the full name shows in the details pane. Every part is set on each Show, so
        // a pooled row never keeps the last adventurer's flag or status color.
        public void Show(Adventurer adventurer, Color rankColor, bool isSelected, RosterReadiness readiness)
        {
            Adventurer = adventurer;

            _badge.style.backgroundColor = rankColor;
            _badgeLabel.Text = RankFormatter.BadgeLabel(adventurer.Rank);
            _name.Text = AdventurerFormatter.GivenName(adventurer.Name);
            ShowReadiness(readiness);
            _classIcon.Set(adventurer.Class.IconFor(adventurer.Rank), adventurer.Class.DisplayName);
            _class.Text = adventurer.Class.DisplayName;

            var status = AdventurerFormatter.RowStatus(adventurer);
            _level.Text = status ?? AdventurerFormatter.ShortLevel(adventurer);
            _level.EnableInClassList(StatusClass, status != null);

            Button.EnableInClassList(SelectedClass, isSelected);
            Button.style.display = DisplayStyle.Flex;
            Button.SetEnabled(true);
        }

        // NOTE: display:none alone keeps the button enabled — keyboard navigation could land on it. Disabled rows are skipped.
        public void Hide()
        {
            Adventurer = null;
            Button.style.display = DisplayStyle.None;
            Button.SetEnabled(false);
        }

        // NOTE: Sprites from GameConfig › Promotion / Advancement › Ready Icon; without one, a framed "P" / "A".
        private void ShowReadiness(RosterReadiness readiness)
        {
            switch (readiness)
            {
                case RosterReadiness.Promotion:
                    _ready.Set(GameConfig.Instance.Promotion.ReadyIcon, "Promote");
                    break;
                case RosterReadiness.Advancement:
                    _ready.Set(GameConfig.Instance.Advancement.ReadyIcon, "Advance");
                    break;
                default:
                    _ready.Hide();
                    break;
            }
        }

        private static OutlinedText Part(string className)
        {
            OutlinedText text = new();
            text.AddToClassList(className);
            return text;
        }
    }
}