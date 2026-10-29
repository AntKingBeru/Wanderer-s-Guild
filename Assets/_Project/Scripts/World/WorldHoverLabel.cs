// The small name label above a hovered guild object (in the world-view document); centred, whole-pixel positioned.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class WorldHoverLabel
    {
        private readonly VisualElement _root;
        private readonly OutlinedText _text;

        private WorldHoverLabel(VisualElement root, OutlinedText text)
        {
            _root = root;
            _text = text;
            Hide();
        }

        public static WorldHoverLabel Bind(VisualElement documentRoot)
        {
            var root = documentRoot?.Q<VisualElement>("world-label");
            var text = documentRoot?.Q<OutlinedText>("world-label-text");
            return root != null && text != null ? new WorldHoverLabel(root, text) : null;
        }

        /// <param name="label"></param>
        /// <param name="localPosition">The anchor in the world view's local space; the label sits centred above it.</param>
        public void Show(string label, Vector2 localPosition)
        {
            _text.Text = label;
            _root.style.left = Mathf.Round(localPosition.x);
            _root.style.top = Mathf.Round(localPosition.y);
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide() => _root.style.display = DisplayStyle.None;
    }
}