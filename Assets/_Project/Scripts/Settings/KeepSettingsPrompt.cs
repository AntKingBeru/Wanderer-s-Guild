using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class KeepSettingsPrompt
    {
        private readonly InlineConfirmation _confirm;
        private readonly VisualElement _clock;
        private IVisualElementScheduledItem _tick;
        private int _remaining;

        /// <param name="confirm"></param>
        /// <param name="clock">Any element of the panel. Its scheduler drives the countdown.</param>
        public KeepSettingsPrompt(InlineConfirmation confirm, VisualElement clock)
        {
            _confirm = confirm;
            _clock = clock;
        }

        public void Show(int seconds, Action onRevert, Action onClosed)
        {
            StopTick();
            _remaining = Mathf.Max(1, seconds);

            // NOTE: Keep is pre-selected (the player just chose this mode); if they can't see it, the timer reverts it.
            _confirm.Show(Message(), onConfirm: null,
                          onClosed: () =>
                          {
                              StopTick();
                              onClosed?.Invoke();
                          },
                          confirmLabel: "Keep", cancelLabel: "Revert", focusConfirm: true, onCancelled: onRevert);

            // NOTE: UI Toolkit's scheduler runs on real time, so the countdown also runs while the game is paused.
            _tick = _clock.schedule.Execute(Tick).Every(1000).StartingIn(1000);
        }

        private void Tick()
        {
            if (!_confirm.IsOpen)
            {
                StopTick();
                return;
            }

            _remaining--;
            if (_remaining <= 0)
            {
                StopTick();
                _confirm.Cancel(); // Timeout = Revert.
                return;
            }

            _confirm.SetMessage(Message());
        }

        private void StopTick()
        {
            _tick?.Pause();
            _tick = null;
        }

        private string Message() => $"Keep these settings?\nReverting in {_remaining}.";
    }
}