using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class ScreenFader : Singleton<ScreenFader>
    {
        private const string OverlayName = "fade-overlay";

        private VisualElement _overlay;
        private Tween _fadeTween;

        // NOTE: Source of truth for the tween. resolvedStyle.opacity lags one layout pass behind inline writes.
        private float _opacity;

        public bool IsFullyBlack => _opacity >= 1f;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _overlay = root?.Q<VisualElement>(OverlayName);

            if (_overlay == null)
            {
                Debug.LogError($"[ScreenFader] Element '{OverlayName}' not found in the UIDocument.", this);
                return;
            }

            ApplyOpacity(_opacity); // Re-apply after the UIDocument rebuilds its tree.
        }

        //Fades to black. Completes when the screen is fully black.
        public Awaitable FadeOutAsync(CancellationToken token)
            => FadeToAsync(1f, GameConfig.Instance.Transitions.FadeOutDuration, token);

        //Fades from black back to the scene.
        public Awaitable FadeInAsync(CancellationToken token)
            => FadeToAsync(0f, GameConfig.Instance.Transitions.FadeInDuration, token);

        public void SetOpacityImmediate(float opacity)
        {
            KillFade();
            ApplyOpacity(opacity);
        }

        private async Awaitable FadeToAsync(float target, float duration, CancellationToken token)
        {
            KillFade(); // A new fade overrides any running one; the old awaiter just returns early.
            _fadeTween = DOTween.To(() => _opacity, ApplyOpacity, target, duration)
                .SetEase(GameConfig.Instance.Transitions.FadeEase)
                .SetUpdate(true) // Unscaled: works while the game is paused.
                .SetLink(gameObject); // Auto-kill if this object is destroyed.

            await _fadeTween.AwaitCompletionAsync(token);
        }

        private void KillFade()
        {
            if (_fadeTween.IsActive())
                _fadeTween.Kill();
        }

        // NOTE: The overlay blocks pointer input only while it is visible.
        private void ApplyOpacity(float value)
        {
            _opacity = Mathf.Clamp01(value);
            if (_overlay == null)
                return;

            _overlay.style.opacity = _opacity;
            _overlay.pickingMode = _opacity > 0f ? PickingMode.Position : PickingMode.Ignore;
        }
    }
}