// HUD gold readout (top-right): coin + amount; on change the number flashes green/red and shakes a few whole pixels.
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class GoldView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly OutlinedText _amount;
        private readonly GameObject _linkTarget;
        private readonly PixelShake _shake;
        private Tween _flashReset;

        // >The gold window — floating numbers start beside it.
        public VisualElement Anchor => _root;

        private GoldView(VisualElement root, VisualElement icon, OutlinedText amount, GameObject linkTarget)
        {
            _root = root;
            _icon = icon;
            _amount = amount;
            _linkTarget = linkTarget;
            _shake = new PixelShake(amount, linkTarget);
        }

        public static GoldView Bind(VisualElement root, GameObject linkTarget)
        {
            var window = root.Q<VisualElement>("gold-root");
            var icon = root.Q<VisualElement>("gold-icon");
            var amount = root.Q<OutlinedText>("gold-amount");
            return window != null && icon != null && amount != null ? new GoldView(window, icon, amount, linkTarget) : null;
        }

        public void SetIcon(Background icon) => _icon.style.backgroundImage = new StyleBackground(icon);

        /// <param name="gold"></param>
        /// <param name="delta"></param>
        /// <param name="animate">False = set silently (initial value, or scene start).</param>
        /// <param name="settings"></param>
        public void SetGold(long gold, long delta, bool animate, GuildHudSettings settings)
        {
            _amount.Text = gold.ToString("N0", CultureInfo.InvariantCulture);
            if (delta == 0 || !animate)
                return;

            Flash(delta > 0 ? settings.GoldGainColor : settings.GoldLossColor, settings.GoldFlashDuration);
            _shake.Play(settings.GoldShakePixels, settings.GoldShakeSteps, settings.GoldShakeStepDuration);
        }

        // NOTE: A hard color switch (pixel-art friendly); StyleKeyword.Null hands the color back to the stylesheet.
        private void Flash(Color color, float duration)
        {
            if (_flashReset.IsActive())
                _flashReset.Kill();

            _amount.SetFillColor(color);
            _flashReset = DOVirtual.DelayedCall(duration, () => _amount.SetFillColor(StyleKeyword.Null), ignoreTimeScale: true)
                .SetLink(_linkTarget);
        }
    }
}