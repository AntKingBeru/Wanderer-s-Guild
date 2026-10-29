// Presenter for the time HUD (MVP): mirrors the Time events onto the view and turns speed-button clicks into commands.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class TimeHudPresenter : MonoBehaviour
    {
        [SerializeField] private TimeHudView view;

        private readonly List<(Button button, System.Action handler)> _wired = new();

        // NOTE: Listeners are added in OnEnable — before any Start — so TimeManager's initial publish is never missed.
        private void OnEnable()
        {
            GameEventsRelay.OnTimeTick.AddListener(OnTimeTick);
            GameEventsRelay.OnDayPhaseChanged.AddListener(OnDayPhaseChanged);
            GameEventsRelay.OnGameSpeedChanged.AddListener(OnGameSpeedChanged);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnTimeTick.RemoveListener(OnTimeTick);
            GameEventsRelay.OnDayPhaseChanged.RemoveListener(OnDayPhaseChanged);
            GameEventsRelay.OnGameSpeedChanged.RemoveListener(OnGameSpeedChanged);
        }

        private void Start()
        {
            if (!view || !view.IsBound)
            {
                Debug.LogError("[TimeHudPresenter] Missing or unbound TimeHudView.", this);
                return;
            }

            foreach (var (speed, value) in view.SpeedButtons)
            {
                System.Action handler = () => OnSpeedClicked(speed);
                value.clicked += handler;
                _wired.Add((value, handler));
            }

            // NOTE: Also syncs if the manager published before this view bound.
            if (TimeManager.HasInstance)
            {
                view.SetTime(TimeManager.Instance.Now);
                view.SetPhase(TimeManager.Instance.Now.Phase);
                view.SetSelectedSpeed(TimeManager.Instance.Speed);
            }
        }

        private void OnDestroy()
        {
            foreach (var (button, handler) in _wired)
                button.clicked -= handler;
            _wired.Clear();
        }

        private void OnTimeTick(GameDateTime time) => view.SetTime(time);
        private void OnDayPhaseChanged(DayPhase phase) => view.SetPhase(phase);
        private void OnGameSpeedChanged(GameSpeed speed) => view.SetSelectedSpeed(speed);

        private void OnSpeedClicked(GameSpeed speed)
        {
            if (!TimeManager.HasInstance)
                return;

            GameEventsRelay.OnUIButtonClicked.Invoke();
            TimeManager.Instance.SetSpeed(speed);
        }
    }
}