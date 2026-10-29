using UnityEngine;

namespace WanderersGuild
{
    public class SceneMusicCue : MonoBehaviour
    {
        [SerializeField] private MusicTrackId track = MusicTrackId.MainMenu;

        [Tooltip("Fade the music out when any scene transition starts from this scene.")]
        [SerializeField] private bool fadeOutOnSceneExit = true;

        private void Start()
        {
            if (!MusicPlayer.HasInstance)
            {
                Debug.LogWarning("[SceneMusicCue] No MusicPlayer found; add it under __PersistentSystems.", this);
                return;
            }

            MusicPlayer.Instance.Play(track);

            if (fadeOutOnSceneExit)
                GameEventsRelay.OnSceneLoadStarted.AddListener(OnSceneLoadStarted);
        }

        private void OnDestroy() => GameEventsRelay.OnSceneLoadStarted.RemoveListener(OnSceneLoadStarted);

        private void OnSceneLoadStarted(SceneId _)
        {
            if (MusicPlayer.HasInstance)
                MusicPlayer.Instance.FadeOut();
        }
    }
}