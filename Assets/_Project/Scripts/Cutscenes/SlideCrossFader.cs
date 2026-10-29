using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class SlideCrossfader
    {
        private readonly GameObject _linkTarget;
        private VisualElement _front;
        private VisualElement _back;
        private Tween _tween;

        public SlideCrossfader(VisualElement layerA, VisualElement layerB, GameObject linkTarget)
        {
            _front = layerA;
            _back = layerB;
            _linkTarget = linkTarget;
        }

        // Fades the slide in on top of the current one, then hides the old layer.
        public async Awaitable ShowAsync(CutsceneSlide slide, float duration, CancellationToken token)
        {
            Kill();

            // NOTE: The hidden layer receives the new slide and is moved to the top of the image stack.
            (_front, _back) = (_back, _front);
            Apply(_front, slide);
            _front.BringToFront();
            _front.style.opacity = 0f;

            var front = _front;
            _tween = DOVirtual.Float(0f, 1f, duration, value => front.style.opacity = value)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(_linkTarget);

            await _tween.AwaitCompletionAsync(token);
            _front.style.opacity = 1f;
            _back.style.opacity = 0f;
        }

        private void Kill()
        {
            if (_tween.IsActive())
                _tween.Kill();
        }

        // NOTE: Placeholder color shows only while a slide has no image.
        private static void Apply(VisualElement layer, CutsceneSlide slide)
        {
            var hasImage = slide.Image;
            layer.style.backgroundImage = hasImage ? new StyleBackground(slide.Image) : new StyleBackground(StyleKeyword.None);
            layer.style.backgroundColor = hasImage ? Color.clear : slide.PlaceholderColor;
        }
    }
}