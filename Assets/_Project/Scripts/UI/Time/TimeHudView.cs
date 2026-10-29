// Binds the time HUD UXML: date, clock, day phase and the four speed buttons (mouse-only, never focusable).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class TimeHudView : MonoBehaviour
    {
        private const string DateName = "hud-date";
        private const string TimeName = "hud-time";
        private const string PhaseName = "hud-phase";
        private const string SelectedClass = "hud-speed--selected";

        private static readonly (GameSpeed speed, string name)[] SpeedButtonNames =
        {
            (GameSpeed.Paused, "hud-speed-pause"),
            (GameSpeed.Normal, "hud-speed-normal"),
            (GameSpeed.Fast, "hud-speed-fast"),
            (GameSpeed.VeryFast, "hud-speed-veryfast")
        };

        private readonly Dictionary<GameSpeed, Button> _speedButtons = new();
        private OutlinedText _date;
        private OutlinedText _time;
        private OutlinedText _phase;

        public bool IsBound { get; private set; }
        public IReadOnlyDictionary<GameSpeed, Button> SpeedButtons => _speedButtons;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _date = root?.Q<OutlinedText>(DateName);
            _time = root?.Q<OutlinedText>(TimeName);
            _phase = root?.Q<OutlinedText>(PhaseName);

            _speedButtons.Clear();
            var allButtons = true;
            foreach (var (speed, name) in SpeedButtonNames)
            {
                var button = root?.Q<Button>(name);
                if (button == null)
                {
                    allButtons = false;
                    continue;
                }

                // NOTE: Never focusable — Space is both UI Submit and the pause hotkey, and would trigger both.
                button.focusable = false;
                button.RemoveFromClassList(Button.ussClassName);
                _speedButtons[speed] = button;
            }

            IsBound = _date != null && _time != null && _phase != null && allButtons;
            if (!IsBound)
                Debug.LogError("[TimeHudView] Missing one of the HUD elements in the UXML.", this);
        }

        public void SetTime(GameDateTime time)
        {
            if (!IsBound)
                return;

            _date.Text = TimeFormatter.Date(time);
            _time.Text = TimeFormatter.Clock(time);
        }

        public void SetPhase(DayPhase phase)
        {
            if (IsBound)
                _phase.Text = TimeFormatter.Phase(phase);
        }

        public void SetSelectedSpeed(GameSpeed speed)
        {
            foreach (var pair in _speedButtons)
                pair.Value.EnableInClassList(SelectedClass, pair.Key == speed);
        }
    }
}