// HUD adventurer counter (under the gold): person icon + "2/6"; amber when full; shakes a few whole pixels on change.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class AdventurerCountView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly OutlinedText _count;
        private readonly PixelShake _shake;

        // The counter window — floating numbers start beside it.
        public VisualElement Anchor => _root;

        private AdventurerCountView(VisualElement root, VisualElement icon, OutlinedText count, GameObject linkTarget)
        {
            _root = root;
            _icon = icon;
            _count = count;
            _shake = new PixelShake(count, linkTarget);
        }

        public static AdventurerCountView Bind(VisualElement root, GameObject linkTarget)
        {
            var window = root.Q<VisualElement>("adv-count-root");
            var icon = root.Q<VisualElement>("adv-count-icon");
            var count = root.Q<OutlinedText>("adv-count-label");
            return window != null && icon != null && count != null ? new AdventurerCountView(window, icon, count, linkTarget) : null;
        }

        public void SetIcon(Background icon) => _icon.style.backgroundImage = new StyleBackground(icon);

        /// <param name="count"></param>
        /// <param name="capacity"></param>
        /// <param name="changed"></param>
        /// <param name="animate">False = set silently (initial value, or scene start).</param>
        /// <param name="settings"></param>
        public void SetCount(int count, int capacity, bool changed, bool animate, GuildHudSettings settings)
        {
            _count.Text = $"{count}/{capacity}";

            // NOTE: StyleKeyword.Null hands the color back to the stylesheet when the guild isn't full.
            if (count >= capacity)
                _count.SetFillColor(settings.AdventurersFullColor);
            else
                _count.SetFillColor(StyleKeyword.Null);

            if (changed && animate)
                _shake.Play(settings.GoldShakePixels, settings.GoldShakeSteps, settings.GoldShakeStepDuration);
        }
    }
}