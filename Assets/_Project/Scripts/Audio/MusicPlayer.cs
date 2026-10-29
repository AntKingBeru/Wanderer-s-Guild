using UnityEngine;

namespace WanderersGuild
{
    public class MusicPlayer : Singleton<MusicPlayer>
    {
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

        //Crossfades to the track. Requesting the track that's already playing does nothing.
        public void Play(MusicTrackId track)
        {
            if (track == MusicTrackId.None)
            {
                FadeOut();
                return;
            }

            if (track == CurrentTrack && _active.IsPlaying)
                return;

            var settings = GameConfig.Instance.Music;
            if (!settings.TryGetTrack(track, out var entry) || !entry.Clip)
            {
                Debug.LogWarning($"[MusicPlayer] No clip assigned for '{track}' in GameConfig › Music; staying silent.", this);
                FadeOut();
                return;
            }

            // NOTE: Swap roles. The old active channel fades out while the other one fades the new track in.
            (_active, _idle) = (_idle, _active);
            _idle.FadeOutAndStop(settings.DefaultFadeOut);
            _active.Play(entry.Clip, entry.Volume, settings.DefaultFadeIn);
            CurrentTrack = track;
        }

        //Fades the current track out. The fade survives scene changes because this object persists.
        public void FadeOut()
        {
            _active.FadeOutAndStop(GameConfig.Instance.Music.DefaultFadeOut);
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