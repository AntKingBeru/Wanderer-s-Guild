// One catalog card: icon, name, and "cost · size · days" (or the lock reason in its place); both lines clip at the card's
// edge (TextLimits.BuildCardLine). Disabled when locked, red cost when unaffordable.
using System;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class BuildCardView
    {
        private const string SelectedClass = "build-card--selected";
        private const string PoorClass = "build-card--poor";
        private const string LineClass = "one-line";

        private readonly VisualElement _icon;
        private readonly OutlinedText _name;
        private readonly OutlinedText _info;

        public Button Button { get; }
        public FacilityData Data { get; private set; }

        // NOTE: Never focusable — Space is UI Submit and the time-pause hotkey.
        public BuildCardView(Action<FacilityData> onPicked)
        {
            Button = new Button(() =>
            {
                if (Data != null)
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
            _name = Line("build-card__name");
            _info = Line("build-card__info");
            text.Add(_name);
            text.Add(_info);

            Button.Add(_icon);
            Button.Add(text);
        }

        // NOTE: Called on every catalog refresh (gold, rank, rooms). A locked card (limit reached) can't be built, so its
        // second line says why instead of the price — and is never red.
        public void Show(BuildCatalogEntry entry, bool isSelected)
        {
            var data = entry.Data;
            Data = data;

            var hasIcon = data.Icon != null;
            _icon.style.backgroundImage = hasIcon ? new StyleBackground(data.Icon) : new StyleBackground(StyleKeyword.None);
            _icon.style.display = hasIcon ? DisplayStyle.Flex : DisplayStyle.None;
            _name.Text = data.DisplayName;
            _info.Text = entry.IsLocked ? entry.LockReason : FacilityFormatter.CardInfo(data);

            Button.EnableInClassList(SelectedClass, isSelected);
            Button.EnableInClassList(PoorClass, entry is { Affordable: false, IsLocked: false });
            Button.SetEnabled(!entry.IsLocked);
            Button.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            Data = null;
            Button.style.display = DisplayStyle.None;
        }

        private static OutlinedText Line(string className)
        {
            OutlinedText text = new() { pickingMode = PickingMode.Ignore };
            text.AddToClassList(className);
            text.AddToClassList(LineClass);
            return text;
        }
    }
}