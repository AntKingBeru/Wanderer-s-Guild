// One pooled row of the armory list: item icon, name, "Slot · price", units left (red when sold out); hidden rows are disabled.
using System;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ArmoryRowView
    {
        private const string SelectedClass = "arm-row--selected";
        private const string SoldOutClass = "arm-row--soldout";

        private readonly IconSlot _icon;
        private readonly OutlinedText _name;
        private readonly OutlinedText _info;
        private readonly OutlinedText _units;

        public Button Button { get; }
        public EquipmentData Item { get; private set; }

        public ArmoryRowView(Action<ArmoryRowView> onClicked)
        {
            Button = new Button(() => onClicked(this));
            Button.AddToClassList("pixel-button");
            Button.AddToClassList("arm-row");

            _icon = IconSlot.Create("arm-row__icon");

            VisualElement text = new() { pickingMode = PickingMode.Ignore };
            text.AddToClassList("arm-row__text");
            _name = Part("arm-row__name");
            _info = Part("arm-row__info");
            text.Add(_name);
            text.Add(_info);

            _units = Part("arm-row__units");
            Button.Add(_icon.Root);
            Button.Add(text);
            Button.Add(_units);
            Hide();
        }

        public void Show(EquipmentData item, int units, bool isSelected)
        {
            Item = item;
            _icon.Set(item.Icon, item.DisplayName);
            _name.Text = item.DisplayName;
            _info.Text = ArmoryFormatter.RowInfo(item);
            _units.Text = ArmoryFormatter.Units(units, item.MaxStock);

            Button.EnableInClassList(SelectedClass, isSelected);
            Button.EnableInClassList(SoldOutClass, units == 0);
            Button.style.display = DisplayStyle.Flex;
            Button.SetEnabled(true);
        }

        // NOTE: display:none alone keeps the button enabled — keyboard navigation could land on it. Disabled rows are skipped.
        public void Hide()
        {
            Item = null;
            Button.style.display = DisplayStyle.None;
            Button.SetEnabled(false);
        }

        private static OutlinedText Part(string className)
        {
            OutlinedText text = new() { pickingMode = PickingMode.Ignore };
            text.AddToClassList(className);
            return text;
        }
    }
}