// A fixed set of speech-bubble views on the world view's "world-bubbles" layer; its size caps the bubbles on screen.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Object Pool — every view is built once at bind; renting/returning only toggles display.
    public sealed class SpeechBubblePool
    {
        private readonly VisualElement _layer;
        private readonly Stack<SpeechBubbleView> _free = new();

        private SpeechBubblePool(VisualElement layer, int size)
        {
            _layer = layer;
            _layer.pickingMode = PickingMode.Ignore; // NOTE: A full-view layer that picked would swallow every world click.
            for (var i = 0; i < size; i++)
                _free.Push(new SpeechBubbleView(layer));
        }

        // Null if the UXML has no "world-bubbles" element.
        public static SpeechBubblePool Bind(VisualElement documentRoot, int size)
        {
            var layer = documentRoot?.Q<VisualElement>("world-bubbles");
            return layer != null ? new SpeechBubblePool(layer, Mathf.Max(1, size)) : null;
        }

        public bool HasFree => _free.Count > 0;

        public bool TryRent(out SpeechBubbleView bubble) => _free.TryPop(out bubble);

        public void Return(SpeechBubbleView bubble)
        {
            if (bubble == null)
                return;
            bubble.Hide();
            _free.Push(bubble);
        }

        // The whole layer (build mode hides it).
        public void SetShown(bool shown) => _layer.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
    }
}