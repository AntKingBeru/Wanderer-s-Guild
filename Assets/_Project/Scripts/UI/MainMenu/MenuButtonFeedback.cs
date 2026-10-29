using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class MenuButtonFeedback : Manipulator
    {
        private readonly GameObject _linkTarget;
        private readonly bool _pop;
        private Tween _scaleTween;
        private float _scale = 1f; // NOTE: Cached tween source; resolvedStyle lags behind inline writes.

        /// <param name="linkTarget"></param>
        /// <param name="pop">False for full-width items in clipped lists, where a scale-up would spill past the edges.</param>
        public MenuButtonFeedback(GameObject linkTarget, bool pop = true)
        {
            _linkTarget = linkTarget;
            _pop = pop;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            target.RegisterCallback<FocusEvent>(OnFocus);
            target.RegisterCallback<BlurEvent>(OnBlur);

            if (target is Button button)
                button.clicked += OnClicked;
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            target.UnregisterCallback<FocusEvent>(OnFocus);
            target.UnregisterCallback<BlurEvent>(OnBlur);

            if (target is Button button)
                button.clicked -= OnClicked;

            KillTween();
        }

        // NOTE: Hover moves focus, so mouse and keyboard share one selected button. The frame swap is USS :focus.
        private void OnPointerEnter(PointerEnterEvent _)
        {
            if (target.enabledInHierarchy && target.focusable)
                target.Focus();
        }

        private void OnFocus(FocusEvent _)
        {
            if (_pop)
            {
                var settings = GameConfig.Instance.MenuButtons;
                TweenScale(settings.PopScale, settings.PopDuration, settings.PopEase);
            }

            GameEventsRelay.OnUIButtonFocused.Invoke();
        }

        private void OnBlur(BlurEvent _)
        {
            if (!_pop)
                return;

            var settings = GameConfig.Instance.MenuButtons;
            TweenScale(1f, settings.SettleDuration, settings.SettleEase);
        }

        private void OnClicked() => GameEventsRelay.OnUIButtonClicked.Invoke();

        private void TweenScale(float to, float duration, Ease ease)
        {
            KillTween();
            _scaleTween = DOTween.To(() => _scale, ApplyScale, to, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        private void ApplyScale(float value)
        {
            _scale = value;
            target.style.scale = new Scale(new Vector3(value, value, 1f));
        }

        private void KillTween()
        {
            if (_scaleTween.IsActive())
                _scaleTween.Kill();
        }
    }
}