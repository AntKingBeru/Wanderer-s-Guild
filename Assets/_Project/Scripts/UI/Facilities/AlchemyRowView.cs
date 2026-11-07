// One pooled row of the alchemy screen: potion name, how many are in stock, and (when brewing) − count + controls.
using System;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class AlchemyRowView
    {
        private readonly OutlinedText _name;
        private readonly OutlinedText _stock;
        private readonly OutlinedText _count;
        private readonly VisualElement _controls;

        public VisualElement Root { get; }
        public Button Minus { get; }
        public Button Plus { get; }
        public PotionKey Key { get; private set; }

        /// <param name="onStep">(row, +1 or −1).</param>
        public AlchemyRowView(Action<AlchemyRowView, int> onStep)
        {
            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            Root.AddToClassList("alc-row");

            _name = Text("alc-row__name");
            _stock = Text("alc-row__stock");

            _controls = new VisualElement { pickingMode = PickingMode.Ignore };
            _controls.AddToClassList("alc-row__controls");
            Minus = StepButton("-", () => onStep(this, -1));
            _count = Text("alc-row__count");
            Plus = StepButton("+", () => onStep(this, +1));
            _controls.Add(Minus);
            _controls.Add(_count);
            _controls.Add(Plus);

            Root.Add(_name);
            Root.Add(_stock);
            Root.Add(_controls);
            Hide();
        }

        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="inStock"></param>
        /// <param name="ordered"></param>
        /// <param name="brewing">Shows − count + (brewing mode); otherwise just the stock count.</param>
        /// <param name="canAdd"></param>
        public void Show(PotionKey key, string name, int inStock, int ordered, bool brewing, bool canAdd)
        {
            Key = key;
            _name.Text = name;
            _stock.Text = brewing ? $"In stock {inStock}" : inStock.ToString();
            _count.Text = ordered.ToString();

            _controls.style.display = brewing ? DisplayStyle.Flex : DisplayStyle.None;
            Minus.SetEnabled(brewing && ordered > 0);
            Plus.SetEnabled(brewing && canAdd);
            Root.style.display = DisplayStyle.Flex;
        }

        // NOTE: Hidden rows disable their buttons, so keyboard navigation skips them.
        public void Hide()
        {
            Key = PotionKey.None;
            Root.style.display = DisplayStyle.None;
            Minus.SetEnabled(false);
            Plus.SetEnabled(false);
        }

        private static Button StepButton(string label, Action onClick)
        {
            Button button = new(onClick);
            button.AddToClassList("pixel-button");
            button.AddToClassList("alc-step");
            OutlinedText text = new() { Text = label, pickingMode = PickingMode.Ignore };
            button.Add(text);
            return button;
        }

        private static OutlinedText Text(string className)
        {
            OutlinedText text = new() { pickingMode = PickingMode.Ignore };
            text.AddToClassList(className);
            return text;
        }
    }
}