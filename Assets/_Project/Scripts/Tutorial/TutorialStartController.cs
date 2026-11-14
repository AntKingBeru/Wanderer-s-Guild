// Opens the play / skip tutorial prompt once a new guild's game has faded in, holding time paused while it's up.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Polls in LateUpdate like the brew prompt — waits for the fade-in (a pause before it would be ignored), a free
    // panel host and no scene load; opens once.
    public class TutorialStartController : MonoBehaviour
    {
        [SerializeField] private GamePanelHost host;

        private bool _opened;
        private bool _ownsPause;

        private void OnEnable() => GameEventsRelay.OnMenuPanelClosed.AddListener(OnPanelClosed);

        private void OnDisable()
        {
            GameEventsRelay.OnMenuPanelClosed.RemoveListener(OnPanelClosed);
            ReleasePause();
        }

        private void LateUpdate()
        {
            if (_opened || host == null || host.IsBusy || host.HasOpenPanel)
                return;
            if (!TutorialManager.HasInstance || TutorialManager.Instance.Stage != TutorialStage.Pending)
                return;
            if (!TimeManager.HasInstance || !TimeManager.Instance.IsRunning)
                return;
            if (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy)
                return;

            _opened = true;
            if (!host.HasPanel(MenuPanelId.TutorialPrompt))
            {
                Debug.LogError("[TutorialStartController] No TutorialPrompt panel on the host — use Debug › Tutorial to skip.", this);
                return;
            }

            TimeManager.Instance.EnterMenuPause(this);
            _ownsPause = true;
            host.Open(MenuPanelId.TutorialPrompt);
        }

        private void OnPanelClosed(MenuPanelId id)
        {
            if (id == MenuPanelId.TutorialPrompt)
                ReleasePause();
        }

        private void ReleasePause()
        {
            if (!_ownsPause)
                return;

            _ownsPause = false;
            if (TimeManager.HasInstance)
                TimeManager.Instance.ExitMenuPause(this);
        }
    }
}