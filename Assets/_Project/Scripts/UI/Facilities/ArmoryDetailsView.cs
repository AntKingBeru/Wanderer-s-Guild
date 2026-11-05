// The armory details pane: name, description, slot, who can use it, stat bonus, price and units in stock.
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ArmoryDetailsView
    {
        private readonly VisualElement _content;
        private readonly OutlinedText _empty;
        private readonly OutlinedText _name;
        private readonly OutlinedText _description;
        private readonly OutlinedText _slot;
        private readonly OutlinedText _users;
        private readonly OutlinedText _stats;
        private readonly OutlinedText _price;
        private readonly OutlinedText _units;

        private ArmoryDetailsView(VisualElement root)
        {
            _content = root.Q<VisualElement>("arm-content");
            _empty = root.Q<OutlinedText>("arm-empty");
            _name = root.Q<OutlinedText>("arm-name");
            _description = root.Q<OutlinedText>("arm-desc");
            _slot = root.Q<OutlinedText>("arm-slot");
            _users = root.Q<OutlinedText>("arm-users");
            _stats = root.Q<OutlinedText>("arm-stats");
            _price = root.Q<OutlinedText>("arm-price");
            _units = root.Q<OutlinedText>("arm-units");
        }

        private bool IsComplete =>
            _content != null && _empty != null && _name != null && _description != null && _slot != null && _users != null &&
            _stats != null && _price != null && _units != null;

        public static ArmoryDetailsView Bind(VisualElement root)
        {
            ArmoryDetailsView view = new(root);
            return view.IsComplete ? view : null;
        }

        public void Show(EquipmentData item, int units, IReadOnlyDictionary<string, ClassData> classes)
        {
            _name.Text = item.DisplayName;
            _description.Text = item.Description;
            _description.style.display = string.IsNullOrEmpty(item.Description) ? DisplayStyle.None : DisplayStyle.Flex;
            _slot.Text = ArmoryFormatter.Slot(item);
            _users.Text = ArmoryFormatter.Users(item, classes);
            _stats.Text = ArmoryFormatter.Stats(item.Stats);
            _price.Text = ArmoryFormatter.Price(item);
            _units.Text = ArmoryFormatter.InStock(units, item.MaxStock);

            _content.style.display = DisplayStyle.Flex;
            _empty.style.display = DisplayStyle.None;
        }

        public void ShowEmpty(string message)
        {
            _empty.Text = message;
            _content.style.display = DisplayStyle.None;
            _empty.style.display = DisplayStyle.Flex;
        }
    }
}