// One "!" mark over the world view: the config sprite at its own pixel size, or an outlined "!" — placed in code, never picks.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class AttentionMarkerView
    {
        private const string RootClass = "attention-marker";
        private const string IconClass = "attention-marker__icon";
        private const string TextClass = "attention-marker__text";
        private const string FallbackMark = "!";

        private readonly VisualElement _root;

        // NOTE: The sprite is drawn 1 sprite pixel = 1 UI pixel; no sprite = the outlined text fallback (WorldView.uss).
        public AttentionMarkerView(VisualElement layer, Sprite icon)
        {
            _root = new VisualElement { pickingMode = PickingMode.Ignore };
            _root.AddToClassList(RootClass);

            if (icon != null)
            {
                VisualElement image = new() { pickingMode = PickingMode.Ignore };
                image.AddToClassList(IconClass);
                image.style.backgroundImage = new StyleBackground(icon);
                image.style.width = icon.rect.width;
                image.style.height = icon.rect.height;
                _root.Add(image);
            }
            else
            {
                OutlinedText text = new() { Text = FallbackMark };
                text.AddToClassList(TextClass);
                _root.Add(text);
            }

            layer.Add(_root);
            Hide();
        }

        // anchor: the mark's bottom-center in the world view's local space (the USS translate centres it above).
        public void Place(Vector2 anchor)
        {
            _root.style.left = Mathf.Round(anchor.x);
            _root.style.top = Mathf.Round(anchor.y);
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide() => _root.style.display = DisplayStyle.None;
    }
}