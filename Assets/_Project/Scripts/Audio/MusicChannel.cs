using DG.Tweening;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class MusicChannel
    {
        private readonly AudioSource _source;
        private readonly GameObject _linkTarget;
        private Tween _fadeTween;

        public MusicChannel(AudioSource source, GameObject linkTarget)
        {
            _source = source;
            _linkTarget = linkTarget;
        }

        public bool IsPlaying => _source.isPlaying;

        //Starts the clip from silence and fades up to the target volume.
        public void Play(AudioClip clip, float targetVolume, float fadeDuration)
        {
            KillFade();
            _source.clip = clip;
            _source.volume = 0f;
            _source.Play();
            _fadeTween = FadeTo(targetVolume, fadeDuration);
        }

        //Fades to silence, then stops. Does nothing when already stopped.
        public void FadeOutAndStop(float fadeDuration)
        {
            if (!_source.isPlaying) return;

            KillFade();
            _fadeTween = FadeTo(0f, fadeDuration).OnComplete(_source.Stop);
        }

        // NOTE: Core DOTween.To (no Audio module needed); unscaled so a pause never freezes a fade.
        private Tween FadeTo(float target, float duration) =>
            DOTween.To(() => _source.volume, value => _source.volume = value, target, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(_linkTarget);

        // NOTE: Kill skips OnComplete, so a pending Stop() can never hit a channel that was just reused.
        private void KillFade()
        {
            if (_fadeTween.IsActive())
                _fadeTween.Kill();
        }
    }
}