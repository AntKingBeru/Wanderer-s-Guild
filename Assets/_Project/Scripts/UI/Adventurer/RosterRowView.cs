// One pooled roster row: rank badge, name (slides when selected and too long), class icon + class, level; hidden rows are
// disabled so keyboard navigation skips them.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RosterRowView
    {
        private const string SelectedClass = "roster-row--selected";

        private readonly VisualElement _badge;
        private readonly OutlinedText _badgeLabel;
        private readonly OutlinedText _name;
        private readonly MarqueeLine _nameMarquee;
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
            _nameMarquee = new MarqueeLine(_name);
            _classIcon = IconSlot.Create("roster-row__class-icon");
            _class = Part("roster-row__class");
            _level = Part("roster-row__level");

            Button.Add(_badge);
            Button.Add(_name);
            Button.Add(_classIcon.Root);
            Button.Add(_class);
            Button.Add(_level);

            Hide();
        }

        // NOTE: Only the selected row's name slides; any other row parks its name at the start (clipped).
        public void Show(Adventurer adventurer, Color rankColor, bool isSelected)
        {
            Adventurer = adventurer;

            _badge.style.backgroundColor = rankColor;
            _badgeLabel.Text = RankFormatter.BadgeLabel(adventurer.Rank);
            _name.Text = adventurer.Name;
            _classIcon.Set(adventurer.Class.Icon, adventurer.Class.DisplayName);
            _class.Text = adventurer.Class.DisplayName;
            _level.Text = AdventurerFormatter.ShortLevel(adventurer);

            Button.EnableInClassList(SelectedClass, isSelected);
            Button.style.display = DisplayStyle.Flex;
            Button.SetEnabled(true);

            if (isSelected)
                _nameMarquee.Refresh();
            else
                _nameMarquee.Stop();
        }

        // NOTE: display:none alone keeps the button enabled — keyboard navigation could land on it. Disabled rows are skipped.
        public void Hide()
        {
            Adventurer = null;
            _nameMarquee.Stop();
            Button.style.display = DisplayStyle.None;
            Button.SetEnabled(false);
        }

        private static OutlinedText Part(string className)
        {
            OutlinedText text = new();
            text.AddToClassList(className);
            return text;
        }
    }
}