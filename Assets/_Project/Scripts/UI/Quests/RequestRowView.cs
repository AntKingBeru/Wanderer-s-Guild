// One pooled list row of the request board: rank badge, title, NEW tag, reward and time left; hidden rows are disabled.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RequestRowView
    {
        private const string RowClass = "request-row";
        private const string SelectedClass = "request-row--selected";
        private const string UrgentClass = "request-row--urgent";

        private readonly VisualElement _badge;
        private readonly OutlinedText _badgeLabel;
        private readonly OutlinedText _title;
        private readonly OutlinedText _new;
        private readonly OutlinedText _reward;
        private readonly OutlinedText _time;

        public Button Button { get; }
        public ActiveRequest Request { get; private set; }

        public RequestRowView(Action<RequestRowView> onClicked)
        {
            Button = new Button(() => onClicked(this));
            Button.AddToClassList("pixel-button");
            Button.AddToClassList(RowClass);

            _badge = new VisualElement { pickingMode = PickingMode.Ignore };
            _badge.AddToClassList("request-row__badge");
            _badgeLabel = new OutlinedText();
            _badge.Add(_badgeLabel);

            _title = Part("request-row__title");
            _new = Part("request-row__new");
            _new.Text = "NEW";
            _reward = Part("request-row__reward");
            _time = Part("request-row__time");

            Button.Add(_badge);
            Button.Add(_title);
            Button.Add(_new);
            Button.Add(_reward);
            Button.Add(_time);

            Hide();
        }

        public void Show(ActiveRequest request, long nowMinutes, Color rankColor, bool isSelected)
        {
            Request = request;
            var data = request.Data;
            var minutesLeft = request.MinutesLeft(nowMinutes);

            _badge.style.backgroundColor = rankColor;
            _badgeLabel.Text = RequestFormatter.BadgeLabel(data.RecommendedRank);
            _title.Text = data.DisplayName;
            _new.style.display = request.IsSeen ? DisplayStyle.None : DisplayStyle.Flex;
            _reward.Text = RequestFormatter.Reward(data.Reward);
            _time.Text = RequestFormatter.TimeLeft(minutesLeft);

            Button.EnableInClassList(UrgentClass, RequestFormatter.IsLastDay(minutesLeft));
            Button.EnableInClassList(SelectedClass, isSelected);
            Button.style.display = DisplayStyle.Flex;
            Button.SetEnabled(true);
        }

        // NOTE: display:none alone keeps the button enabled — keyboard navigation could land on it. Disabled rows are skipped.
        public void Hide()
        {
            Request = null;
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