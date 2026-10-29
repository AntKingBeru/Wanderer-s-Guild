// Keyboard shortcuts for game speed — toggle pause, Normal, Fast, Very Fast — via InputActionReferences in GameConfig.
using UnityEngine;

namespace WanderersGuild
{
    public class TimeControlsInput : MonoBehaviour
    {
        private ScopedActionListener[] _listeners;

        private void OnEnable()
        {
            var bindings = GameConfig.Instance.InputBindings;
            _listeners = new[]
            {
                new ScopedActionListener(bindings.TogglePause, _ => WithManager(manager => manager.TogglePause())),
                new ScopedActionListener(bindings.SpeedNormal, _ => WithManager(manager => manager.SetSpeed(GameSpeed.Normal))),
                new ScopedActionListener(bindings.SpeedFast, _ => WithManager(manager => manager.SetSpeed(GameSpeed.Fast))),
                new ScopedActionListener(bindings.SpeedVeryFast, _ => WithManager(manager => manager.SetSpeed(GameSpeed.VeryFast)))
            };
        }

        private void OnDisable()
        {
            if (_listeners == null)
                return;

            foreach (var listener in _listeners)
                listener.Dispose();
            _listeners = null;
        }

        private static void WithManager(System.Action<TimeManager> action)
        {
            if (TimeManager.HasInstance)
                action(TimeManager.Instance);
        }
    }
}