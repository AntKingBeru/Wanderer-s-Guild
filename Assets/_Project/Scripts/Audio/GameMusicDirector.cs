// Game scene music: the day or night track by the day phase, crossfading slowly when the phase turns; fades out on leaving.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Observer — TimeManager raises OnDayPhaseChanged once in its Start and at every change, so this subscribes in
    // OnEnable to catch the first one. The scene's first track uses the normal fades; later turns use the slow crossfade.
    public class GameMusicDirector : MonoBehaviour
    {
        private bool _hasStarted; // NOTE: False until the first track of this scene is requested.
        private bool _isLeaving;  // NOTE: Set once a scene load starts — phase changes after that never restart music.

        private void OnEnable()
        {
            _hasStarted = false;
            _isLeaving = false;
            GameEventsRelay.OnDayPhaseChanged.AddListener(OnDayPhaseChanged);
            GameEventsRelay.OnSceneLoadStarted.AddListener(OnSceneLoadStarted);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnDayPhaseChanged.RemoveListener(OnDayPhaseChanged);
            GameEventsRelay.OnSceneLoadStarted.RemoveListener(OnSceneLoadStarted);
        }

        private void Start()
        {
            if (!MusicPlayer.HasInstance)
                Debug.LogWarning("[GameMusicDirector] No MusicPlayer (start from Bootstrap); the game stays silent.", this);
        }

        private void OnDayPhaseChanged(DayPhase phase)
        {
            if (_isLeaving || !MusicPlayer.HasInstance)
                return;

            var settings = GameConfig.Instance.Music;
            var track = settings.GameTrackFor(phase);
            if (_hasStarted)
                MusicPlayer.Instance.Play(track, settings.DayNightCrossfade, settings.DayNightCrossfade);
            else
                MusicPlayer.Instance.Play(track);
            _hasStarted = true;
        }

        // NOTE: Save & Quit / loading a slot: fades with the screen; the next scene starts its own track.
        private void OnSceneLoadStarted(SceneId _)
        {
            _isLeaving = true;
            if (MusicPlayer.HasInstance)
                MusicPlayer.Instance.FadeOut();
        }
    }
}