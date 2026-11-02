// One catalog card: icon, name, "cost · size · days", and the lock reason; disabled when locked, red cost when unaffordable.
using System;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class BuildCardView
    {
        private const string SelectedClass = "build-card--selected";
        private const string PoorClass = "build-card--poor";

        private readonly VisualElement _icon;
        private readonly OutlinedText _name;
        private readonly OutlinedText _info;
        private readonly OutlinedText _reason;

        public Button Button { get; }
        public FacilityData Data { get; private set; }

        // NOTE: Never focusable — Space is UI Submit and the time-pause hotkey.
        public BuildCardView(Action<FacilityData> onPicked)
        {
            Button = new Button(() =>
            {
                if (Data)
                    onPicked(Data);
            })
            {
                focusable = false
            };
            Button.AddToClassList("pixel-button");
            Button.AddToClassList("build-card");

            _icon = new VisualElement { pickingMode = PickingMode.Ignore };
            _icon.AddToClassList("build-card__icon");

            VisualElement text = new() { pickingMode = PickingMode.Ignore };
            text.AddToClassList("build-card__text");
            _name = Part("build-card__name");
            _info = Part("build-card__info");
            _reason = Part("build-card__reason");
            text.Add(_name);
            text.Add(_info);
            text.Add(_reason);

            Button.Add(_icon);
            Button.Add(text);
        }

        public void Show(BuildCatalogEntry entry, bool isSelected)
        {
            var data = entry.Data;
            Data = data;

            _icon.style.backgroundImage = data.Icon ? new StyleBackground(data.Icon) : new StyleBackground(StyleKeyword.None);
            _icon.style.display = data.Icon ? DisplayStyle.Flex : DisplayStyle.None;
            _name.Text = data.DisplayName;

            var days = data.BuildDaysFor(1);
            _info.Text = $"{data.CostFor(1):N0}g · {data.Size.x}x{data.Size.y} · {days}d";
            _reason.Text = entry.LockReason;
            _reason.style.display = entry.IsLocked ? DisplayStyle.Flex : DisplayStyle.None;

            Button.EnableInClassList(SelectedClass, isSelected);
            Button.EnableInClassList(PoorClass, !entry.Affordable);
            Button.SetEnabled(!entry.IsLocked);
            Button.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            Data = null;
            Button.style.display = DisplayStyle.None;
        }

        private static OutlinedText Part(string className)
        {
            OutlinedText text = new() { pickingMode = PickingMode.Ignore };
            text.AddToClassList(className);
            return text;
        }
    }
}