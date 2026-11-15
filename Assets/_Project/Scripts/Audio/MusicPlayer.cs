// Persistent music player: crossfades between two channels when the track changes, with default or custom fade times.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public class MusicPlayer : Singleton<MusicPlayer>
    {
        private readonly HashSet<MusicTrackId> _warnedMissing = new();
        private MusicChannel _active;
        private MusicChannel _idle;

        //The track playing or fading in. None while silent or fading out.
        public MusicTrackId CurrentTrack { get; private set; } = MusicTrackId.None;

        protected override void OnSingletonAwake()
        {
            var settings = GameConfig.Instance.Music;
            _active = new MusicChannel(CreateSource(settings), gameObject);
            _idle = new MusicChannel(CreateSource(settings), gameObject);
        }

        // Crossfades to the track with the default fades. Requesting the track that's already playing does nothing.
        public void Play(MusicTrackId track)
        {
            var settings = GameConfig.Instance.Music;
            Play(track, settings.DefaultFadeIn, settings.DefaultFadeOut);
        }

        // Crossfades to the track: the new one fades in over fadeIn while the old one fades out over fadeOut.
        public void Play(MusicTrackId track, float fadeIn, float fadeOut)
        {
            if (track == MusicTrackId.None)
            {
                FadeOut(fadeOut);
                return;
            }

            if (track == CurrentTrack && _active.IsPlaying)
                return;

            if (!GameConfig.Instance.Music.TryGetTrack(track, out var entry) || entry.Clip == null)
            {
                // NOTE: Once per track — the game scene asks again at every phase change.
                if (_warnedMissing.Add(track))
                    Debug.LogWarning($"[MusicPlayer] No clip assigned for '{track}' in GameConfig › Music; staying silent.", this);
                FadeOut(fadeOut);
                return;
            }

            // NOTE: Swap roles. The old active channel fades out while the other one fades the new track in.
            (_active, _idle) = (_idle, _active);
            _idle.FadeOutAndStop(fadeOut);
            _active.Play(entry.Clip, entry.Volume, fadeIn, !entry.PlayOnce);
            CurrentTrack = track;
        }

        //Fades the current track out. The fade survives scene changes because this object persists.
        public void FadeOut() => FadeOut(GameConfig.Instance.Music.DefaultFadeOut);

        public void FadeOut(float duration)
        {
            _active.FadeOutAndStop(duration);
            CurrentTrack = MusicTrackId.None;
        }

        private AudioSource CreateSource(MusicSettings settings)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.ignoreListenerPause = true;
            source.outputAudioMixerGroup = settings.OutputGroup;
            source.volume = 0f;
            return source;
        }
    }
}