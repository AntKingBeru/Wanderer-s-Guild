using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class BlinkingPrompt
    {
        private readonly VisualElement _element;
        private readonly GameObject _linkTarget;
        private Sequence _blink;
        private bool _visible;

        public BlinkingPrompt(VisualElement element, GameObject linkTarget)
        {
            _element = element;
            _linkTarget = linkTarget;
            _element.style.opacity = 0f;
        }

        public void SetVisible(bool visible, float interval)
        {
            if (visible == _visible)
                return;

            _visible = visible;
            Kill();
            _element.style.opacity = visible ? 1f : 0f;
            if (!visible)
                return;

            _blink = DOTween.Sequence()
                .AppendInterval(interval)
                .AppendCallback(() => _element.style.opacity = 0f)
                .AppendInterval(interval)
                .AppendCallback(() => _element.style.opacity = 1f)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        private void Kill()
        {
            if (_blink.IsActive())
                _blink.Kill();
        }
    }
}