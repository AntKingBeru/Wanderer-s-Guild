// Shakes a UI element by alternating whole-pixel offsets (translate, no layout change), always ending at rest.
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class PixelShake
    {
        private readonly VisualElement _target;
        private readonly GameObject _linkTarget;
        private Sequence _sequence;

        public PixelShake(VisualElement target, GameObject linkTarget)
        {
            _target = target;
            _linkTarget = linkTarget;
        }

        // NOTE: A new shake replaces a running one; the last step returns the element to 0.
        public void Play(int pixels, int steps, float stepDuration)
        {
            if (_sequence.IsActive())
                _sequence.Kill();

            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(_linkTarget);
            for (var step = 0; step < steps; step++)
            {
                float offset = step % 2 == 0 ? pixels : -pixels;
                _sequence.AppendCallback(() => SetOffset(offset));
                _sequence.AppendInterval(stepDuration);
            }
            _sequence.AppendCallback(() => SetOffset(0f));
        }

        private void SetOffset(float x) => _target.style.translate = new Translate(x, 0f);
    }
}