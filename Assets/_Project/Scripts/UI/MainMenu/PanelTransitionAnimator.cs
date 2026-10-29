using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class PanelTransitionAnimator
    {
        private readonly GameObject _linkTarget;
        private Tween _tween;

        public PanelTransitionAnimator(GameObject linkTarget) => _linkTarget = linkTarget;

        // Opening: 0 → 1 (fade in, window rises into place). Closing: 1 → 0.
        public async Awaitable PlayAsync(VisualElement root, VisualElement window, bool opening, CancellationToken token)
        {
            KillTween();

            var settings = GameConfig.Instance.PanelTransitions;
            var from = opening ? 0f : 1f;
            var to = opening ? 1f : 0f;

            Apply(root, window, from, settings.WindowSlide);
            _tween = DOVirtual.Float(from, to, opening ? settings.OpenDuration : settings.CloseDuration,
                    t => Apply(root, window, t, settings.WindowSlide))
                .SetEase(opening ? settings.OpenEase : settings.CloseEase)
                .SetUpdate(true)
                .SetLink(_linkTarget);

            await _tween.AwaitCompletionAsync(token);
            Apply(root, window, to, settings.WindowSlide); // NOTE: Exact end state even if the tween was killed.
        }

        private static void Apply(VisualElement root, VisualElement window, float t, float slide)
        {
            root.style.opacity = t;
            window.style.translate = new Translate(new Length(0f), new Length(slide * (1f - t)));
        }

        private void KillTween()
        {
            if (_tween.IsActive())
                _tween.Kill();
        }
    }
}