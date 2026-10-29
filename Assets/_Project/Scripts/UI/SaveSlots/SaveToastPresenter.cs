// Shows "Saving..." / "Saved" / "Save failed" for manual saves and auto-saves (exit saves are silent).
using UnityEngine;

namespace WanderersGuild
{
    public class SaveToastPresenter : MonoBehaviour
    {
        [SerializeField] private SaveToastView view;

        private void OnEnable()
        {
            GameEventsRelay.OnGameSaveStarted.AddListener(OnSaveStarted);
            GameEventsRelay.OnGameSaveCompleted.AddListener(OnSaveCompleted);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnGameSaveStarted.RemoveListener(OnSaveStarted);
            GameEventsRelay.OnGameSaveCompleted.RemoveListener(OnSaveCompleted);
        }

        private void OnSaveStarted(SaveReason reason)
        {
            if (reason == SaveReason.Exit || !view)
                return;
            view.Show("Saving...", false, -1f);
        }

        private void OnSaveCompleted(SaveReason reason, bool saved)
        {
            if (reason == SaveReason.Exit || !view)
                return;

            var settings = GameConfig.Instance.SaveGame;
            if (saved)
                view.Show("Saved", false, settings.SavedToastDuration);
            else
                view.Show("Save failed", true, settings.FailedToastDuration);
        }
    }
}