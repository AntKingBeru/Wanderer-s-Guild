// Reveals text one character at a time into an IRevealTarget, on unscaled time.
using DG.Tweening;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class TypewriterText
    {
        private readonly IRevealTarget _target;
        private readonly GameObject _linkTarget;
        private Tween _tween;
        private string _full = string.Empty;
        private int _shown;

        public TypewriterText(IRevealTarget target, GameObject linkTarget)
        {
            _target = target;
            _linkTarget = linkTarget;
        }

        public bool IsTyping => _shown < _full.Length;

        // Starts typing the text from the first character.
        public void Play(string text, float charactersPerSecond)
        {
            Kill();
            _full = text ?? string.Empty;
            _shown = 0;
            _target.Clear();
            if (_full.Length == 0)
                return;

            var duration = _full.Length / Mathf.Max(1f, charactersPerSecond);
            _tween = DOVirtual.Float(0f, _full.Length, duration, value => Reveal(Mathf.FloorToInt(value)))
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(_linkTarget)
                .OnComplete(() => Reveal(_full.Length));
        }

        // Shows the whole text at once (the player pressed a key mid-typing).
        public void Complete()
        {
            Kill();
            Reveal(_full.Length);
        }

        public void Clear()
        {
            Kill();
            _full = string.Empty;
            _shown = 0;
            _target.Clear();
        }

        // Stops typing where it is (used when playback ends).
        public void Stop() => Kill();

        // NOTE: Only pushes to the target when the count changes — the tween updates every frame.
        private void Reveal(int count)
        {
            var clamped = Mathf.Clamp(count, 0, _full.Length);
            if (clamped == _shown)
                return;

            _shown = clamped;
            _target.Show(_full, _shown);
        }

        private void Kill()
        {
            if (_tween.IsActive())
                _tween.Kill();
        }
    }
}