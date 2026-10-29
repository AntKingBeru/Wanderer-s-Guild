// Counts real time played in the Game scene (paused time included, fade-in excluded) and writes it into saves.
using UnityEngine;

namespace WanderersGuild
{
    public class PlayTimeTracker : MonoBehaviour, ISaveContributor
    {
        private double _seconds;

        public double Seconds => _seconds;

        private void Start()
        {
            if (!SaveGameService.HasInstance) return;

            _seconds = SaveGameService.Instance.StartPlayTimeSeconds;
            SaveGameService.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
        }

        // NOTE: Counts only once time is running (after the fade-in), on unscaled time — so pausing still counts as played.
        private void Update()
        {
            if (TimeManager.HasInstance && TimeManager.Instance.IsRunning)
                _seconds += Time.unscaledDeltaTime;
        }

        public void WriteTo(GameSaveData data) => data.PlayTimeSeconds = _seconds;
    }
}