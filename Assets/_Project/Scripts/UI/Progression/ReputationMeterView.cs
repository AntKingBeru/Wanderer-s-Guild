// HUD reputation (bottom-left): the level emoji is always shown; the vertical bar fades in on a change, fills, holds, fades.
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ReputationMeterView
    {
        private const string IconName = "rep-icon";
        private const string BarName = "rep-bar";
        private const string FillName = "rep-fill";

        private readonly VisualElement _icon;
        private readonly VisualElement _bar;
        private readonly VisualElement _fill;
        private readonly GameObject _linkTarget;
        private Sequence _sequence;
        private float _value; // 0–1
        private float _opacity;

        private ReputationMeterView(VisualElement icon, VisualElement bar, VisualElement fill, GameObject linkTarget)
        {
            _icon = icon;
            _bar = bar;
            _fill = fill;
            _linkTarget = linkTarget;

            // NOTE: The bar has no size until layout runs; re-apply the stored value once it does.
            _bar.RegisterCallback<GeometryChangedEvent>(_ => ApplyValue(_value));
            ApplyOpacity(0f);
        }

        public static ReputationMeterView Bind(VisualElement root, GameObject linkTarget)
        {
            var icon = root.Q<VisualElement>(IconName);
            var bar = root.Q<VisualElement>(BarName);
            var fill = root.Q<VisualElement>(FillName);
            return icon != null && bar != null && fill != null ? new ReputationMeterView(icon, bar, fill, linkTarget) : null;
        }

        // Emoji for the level, tinted in the level's color (the bar fill uses the same color).
        public void SetLevel(Background icon, Color color)
        {
            _icon.style.backgroundImage = new StyleBackground(icon);
            _icon.style.unityBackgroundImageTintColor = color;
            _fill.style.backgroundColor = color;
        }

        // Sets the value without showing the bar (scene start).
        public void SetValueImmediate(float normalized)
        {
            Kill();
            ApplyValue(Mathf.Clamp01(normalized));
        }

        // Fades the bar in, animates to the new value, holds, then fades out. A new change restarts the hold.
        public void ShowChange(float normalized, GuildHudSettings settings)
        {
            Kill();
            var target = Mathf.Clamp01(normalized);

            _sequence = DOTween.Sequence()
                .Append(DOVirtual.Float(_opacity, 1f, settings.ReputationFadeDuration, ApplyOpacity))
                .Join(DOVirtual.Float(_value, target, settings.ReputationFillDuration, ApplyValue).SetEase(Ease.OutCubic))
                .AppendInterval(settings.ReputationShowDuration)
                .Append(DOVirtual.Float(1f, 0f, settings.ReputationFadeDuration, ApplyOpacity))
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        // NOTE: Fills bottom-up in whole UI pixels.
        private void ApplyValue(float value)
        {
            _value = value;

            var height = _bar.contentRect.height;
            if (float.IsNaN(height) || height <= 0f)
                return;

            _fill.style.height = Mathf.Round(value * height);
        }

        private void ApplyOpacity(float opacity)
        {
            _opacity = Mathf.Clamp01(opacity);
            _bar.style.opacity = _opacity;
        }

        private void Kill()
        {
            if (_sequence.IsActive())
                _sequence.Kill();
        }
    }
}