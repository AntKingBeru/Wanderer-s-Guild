// A 12×12 icon element: draws a sprite, or a framed one-letter placeholder when there's none (pooled rows re-set both).
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class IconSlot
    {
        private const string SlotClass = "icon-slot";
        private const string PlaceholderClass = "icon-slot--placeholder";
        private const string LetterClass = "icon-slot__letter";

        private readonly OutlinedText _letter;

        public VisualElement Root { get; }

        private IconSlot(VisualElement root)
        {
            Root = root;
            Root.pickingMode = PickingMode.Ignore;
            Root.AddToClassList(SlotClass);

            _letter = new OutlinedText { pickingMode = PickingMode.Ignore };
            _letter.AddToClassList(LetterClass);
            Root.Add(_letter);
        }

        // A new slot (for rows built in code), with an extra class for its own spacing.
        public static IconSlot Create(string extraClass = null)
        {
            VisualElement root = new();
            if (!string.IsNullOrEmpty(extraClass))
                root.AddToClassList(extraClass);
            return new IconSlot(root);
        }

        // Wraps an element from UXML (by name), or null if it's missing.
        public static IconSlot Bind(VisualElement documentRoot, string name)
        {
            var element = documentRoot?.Q<VisualElement>(name);
            return element != null ? new IconSlot(element) : null;
        }

        // NOTE: Always sets both states, so a reused row never keeps the previous sprite or letter.
        public void Set(Sprite sprite, string nameForPlaceholder)
        {
            Root.style.backgroundImage = sprite ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
            Root.EnableInClassList(PlaceholderClass, sprite == null);
            _letter.Text = sprite ? string.Empty : Initial(nameForPlaceholder);
            Root.style.display = DisplayStyle.Flex;
            Root.style.visibility = Visibility.Visible;
        }

        // Removed from the layout (neighbors close the gap).
        public void Hide() => Root.style.display = DisplayStyle.None;

        // Invisible but still taking its space (neighbors don't shift).
        public void Blank()
        {
            Root.style.display = DisplayStyle.Flex;
            Root.style.visibility = Visibility.Hidden;
        }

        // NOTE: The first letter or digit of the name, uppercased ("?" if none) — one glyph fits 12px.
        private static string Initial(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                foreach (var c in name.Where(char.IsLetterOrDigit))
                {
                    return char.ToUpperInvariant(c).ToString();
                }
            }
            return "?";
        }
    }
}