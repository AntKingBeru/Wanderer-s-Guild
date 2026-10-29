// Boot step: plays the studio splash slides (fade in, hold, fade out); pressing Skip skips the rest of the splash.
using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    public class SplashSequenceStep : BootstrapStep
    {
        [SerializeField] private SplashView view;

        private Sequence _activeSequence;
        private bool _skipRequested;

        public override async Awaitable ExecuteAsync(CancellationToken token)
        {
            if (!view || !view.IsBound)
            {
                Debug.LogWarning("[SplashSequenceStep] No bound SplashView; skipping splash.", this);
                return;
            }

            _skipRequested = false;

            // NOTE: The using scope guarantees the skip listener is removed even if the step is canceled.
            using (new ScopedActionListener(GameConfig.Instance.InputBindings.Skip, OnSkipPerformed))
            {
                foreach (var slide in GameConfig.Instance.Splash.Slides)
                {
                    if (_skipRequested)
                        break;

                    view.ShowSlide(slide);
                    view.ContentOpacity = 0f;
                    _activeSequence = BuildSlideSequence(slide);
                    await _activeSequence.AwaitCompletionAsync(token);
                }
            }
        }

        // NOTE: Killing the sequence makes AwaitCompletionAsync return; the scene fader then covers the cut.
        private void OnSkipPerformed(InputAction.CallbackContext _)
        {
            _skipRequested = true;
            if (_activeSequence.IsActive())
                _activeSequence.Kill();
        }

        // NOTE: DOVirtual.Float uses explicit from/to values. Getter-based nested tweens could capture stale start values.
        private Sequence BuildSlideSequence(SplashSlide slide)
        {
            return DOTween.Sequence()
                .AppendInterval(slide.DelayBefore)
                .Append(DOVirtual.Float(0f, 1f, slide.FadeInDuration, SetOpacity).SetEase(Ease.OutSine))
                .AppendInterval(slide.HoldDuration)
                .Append(DOVirtual.Float(1f, 0f, slide.FadeOutDuration, SetOpacity).SetEase(Ease.InSine))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void SetOpacity(float value) => view.ContentOpacity = value;
    }
}