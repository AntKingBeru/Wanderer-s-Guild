// Plays a sequence of whole-pixel offsets on a sprite (no in-between positions), then returns it to rest.
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class PixelStepAnimation
    {
        private readonly Transform _target;
        private readonly Vector3 _restLocalPosition;
        private readonly float _pixelsPerUnit;
        private readonly GameObject _linkTarget;
        private Sequence _sequence;

        /// <param name="target"></param>
        /// <param name="pixelsPerUnit">The sprite's PPU, so 1 step = 1 sprite pixel at any scale.</param>
        /// <param name="linkTarget"></param>
        public PixelStepAnimation(Transform target, float pixelsPerUnit, GameObject linkTarget)
        {
            _target = target;
            _restLocalPosition = target.localPosition;
            _pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
            _linkTarget = linkTarget;
        }

        // Restarts from rest, shows each step for its duration, then snaps back to rest.
        public void Play(IReadOnlyList<PixelStep> steps)
        {
            Kill();
            Apply(Vector2Int.zero);

            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(_linkTarget);
            foreach (var step in steps)
            {
                var offset = step.Offset;
                _sequence.AppendCallback(() => Apply(offset));
                _sequence.AppendInterval(step.Duration);
            }
            _sequence.AppendCallback(() => Apply(Vector2Int.zero));
        }

        private void Apply(Vector2Int offset) =>
            _target.localPosition = _restLocalPosition + new Vector3(offset.x, offset.y, 0f) / _pixelsPerUnit;

        private void Kill()
        {
            if (_sequence.IsActive())
                _sequence.Kill();
        }
    }
}