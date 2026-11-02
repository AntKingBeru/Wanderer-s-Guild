// One pooled row of the Recruits list: name, "Class Lv N", time left (red on the last day); hidden rows are disabled.
using System;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RecruitRowView
    {
        private const string SelectedClass = "rec-row--selected";
        private const string UrgentClass = "rec-row--urgent";

        private readonly OutlinedText _name;
        private readonly OutlinedText _info;
        private readonly OutlinedText _time;

        public Button Button { get; }
        public Applicant Applicant { get; private set; }

        public RecruitRowView(Action<RecruitRowView> onClicked)
        {
            Button = new Button(() => onClicked(this));
            Button.AddToClassList("pixel-button");
            Button.AddToClassList("rec-row");

            VisualElement text = new() { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rec-row__text");
            _name = Part("rec-row__name");
            _info = Part("rec-row__info");
            text.Add(_name);
            text.Add(_info);

            _time = Part("rec-row__time");
            Button.Add(text);
            Button.Add(_time);
            Hide();
        }

        public void Show(Applicant applicant, long nowMinutes, bool isSelected)
        {
            Applicant = applicant;
            _name.Text = applicant.Adventurer.Name;
            _info.Text = RecruitFormatter.RowInfo(applicant.Adventurer);
            _time.Text = RecruitFormatter.TimeLeft(applicant, nowMinutes);

            Button.EnableInClassList(SelectedClass, isSelected);
            Button.EnableInClassList(UrgentClass, RecruitFormatter.IsLastDay(applicant, nowMinutes));
            Button.style.display = DisplayStyle.Flex;
            Button.SetEnabled(true);
        }

        // NOTE: display:none alone keeps the button enabled — keyboard navigation could land on it. Disabled rows are skipped.
        public void Hide()
        {
            Applicant = null;
            Button.style.display = DisplayStyle.None;
            Button.SetEnabled(false);
        }

        private static OutlinedText Part(string className)
        {
            OutlinedText text = new()
            {
                pickingMode = PickingMode.Ignore
            };
            text.AddToClassList(className);
            return text;
        }
    }
}