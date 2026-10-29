// Hold-to-skip: while active, holding the skip key fills a bar (it drains on release); a full bar fires the skip once.
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    public class HoldToSkip : MonoBehaviour
    {
        [SerializeField] private CutsceneView view;

        private InputAction _action;
        private Action _onSkip;
        private float _progress;
        private bool _isActive;

        // >True while any control bound to the skip action is held (read straight from the bound controls).
        public bool IsSkipKeyPressed
        {
            get
            {
                if (_action == null)
                    return false;

                return _action.controls.Any(control => control.IsPressed());
            }
        }

        // NOTE: Shares the Skip action with the splash (which reacts to the press); here it's polled as a hold.
        public void Activate(Action onSkip)
        {
            var reference = GameConfig.Instance.InputBindings.Skip;
            _action = reference ? reference.action : null;

            if (_action == null)
            {
                Debug.LogWarning("[HoldToSkip] No Skip action in GameConfig › Input Bindings; skipping is off.", this);
                return;
            }

            // NOTE: Enables if needed but never disables — the action is shared.
            if (!_action.enabled)
                _action.Enable();

            _onSkip = onSkip;
            _progress = 0f;
            _isActive = true;

            // NOTE: Shows the bound key's real name (e.g. "Esc"), so a rebind updates the hint.
            if (view)
                view.ShowSkipHint($"Hold {_action.GetBindingDisplayString()} to skip");
        }

        public void Deactivate()
        {
            _isActive = false;
            _onSkip = null;
            _progress = 0f;

            if (view)
                view.HideSkipHint();
        }

        // NOTE: Unscaled time keeps the hold length the same at any frame rate or timescale.
        private void Update()
        {
            if (!_isActive)
                return;

            var settings = GameConfig.Instance.Intro;
            var step = Time.unscaledDeltaTime / settings.SkipHoldDuration;
            _progress = Mathf.Clamp01(_progress + (IsSkipKeyPressed ? step : -step * settings.SkipDrainMultiplier));

            if (view)
                view.SetSkipProgress(_progress);

            if (_progress < 1f)
                return;

            var skip = _onSkip;
            Deactivate();
            skip?.Invoke();
        }
    }
}