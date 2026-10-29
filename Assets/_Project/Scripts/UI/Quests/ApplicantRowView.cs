// One pooled applicant row: "Name · Class · Lv · Rank" with its own Decline button; hidden rows are disabled.
using System;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ApplicantRowView
    {
        private readonly OutlinedText _label;

        public VisualElement Root { get; }
        public Button Decline { get; }
        public Adventurer Adventurer { get; private set; }

        public ApplicantRowView(Action<Adventurer> onDecline)
        {
            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            Root.AddToClassList("applicant-row");

            _label = new OutlinedText();
            _label.AddToClassList("applicant-row__label");

            Decline = new Button(() => { if (Adventurer != null) onDecline(Adventurer); });
            Decline.AddToClassList("pixel-button");
            Decline.AddToClassList("applicant-row__decline");
            OutlinedText text = new() { Text = "Decline" };
            Decline.Add(text);

            Root.Add(_label);
            Root.Add(Decline);
            Hide();
        }

        public void Show(Adventurer adventurer)
        {
            Adventurer = adventurer;
            _label.Text = ApplicationFormatter.Applicant(adventurer);
            Root.style.display = DisplayStyle.Flex;
            Decline.SetEnabled(true);
        }

        public void Hide()
        {
            Adventurer = null;
            Root.style.display = DisplayStyle.None;
            Decline.SetEnabled(false);
        }
    }
}