// One pooled row of the applications list: rank badge, quest title, status ("2/3" or READY); hidden rows are disabled.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ApplicationRowView
    {
        private const string SelectedClass = "app-row--selected";
        private const string ReadyClass = "app-row--ready";

        private readonly VisualElement _badge;
        private readonly OutlinedText _badgeLabel;
        private readonly OutlinedText _title;
        private readonly OutlinedText _status;

        public Button Button { get; }
        public Quest Quest { get; private set; }

        public ApplicationRowView(Action<ApplicationRowView> onClicked)
        {
            Button = new Button(() => onClicked(this));
            Button.AddToClassList("pixel-button");
            Button.AddToClassList("app-row");

            _badge = new VisualElement { pickingMode = PickingMode.Ignore };
            _badge.AddToClassList("app-row__badge");
            _badgeLabel = new OutlinedText();
            _badge.Add(_badgeLabel);

            _title = Part("app-row__title");
            _status = Part("app-row__status");

            Button.Add(_badge);
            Button.Add(_title);
            Button.Add(_status);

            Hide();
        }

        public void Show(Quest quest, Color rankColor, bool isSelected)
        {
            Quest = quest;

            _badge.style.backgroundColor = rankColor;
            _badgeLabel.Text = RankFormatter.BadgeLabel(quest.Request.RecommendedRank);
            _title.Text = quest.Request.DisplayName;
            _status.Text = ApplicationFormatter.RowStatus(quest);

            Button.EnableInClassList(ReadyClass, quest.State == QuestState.Ready);
            Button.EnableInClassList(SelectedClass, isSelected);
            Button.style.display = DisplayStyle.Flex;
            Button.SetEnabled(true);
        }

        // NOTE: display:none alone keeps the button enabled — keyboard navigation could land on it. Disabled rows are skipped.
        public void Hide()
        {
            Quest = null;
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