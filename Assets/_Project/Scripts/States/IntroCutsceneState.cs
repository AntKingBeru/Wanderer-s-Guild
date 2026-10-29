using System;
using System.Threading;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    public sealed class IntroCutsceneState : IState
    {
        private readonly CutscenePlayer _player;
        private readonly HoldToSkip _holdToSkip;
        private readonly CutsceneData _data;
        private readonly InputActionReference _advanceAction;
        private readonly Action<bool> _onFinished;
        private readonly CancellationToken _token;
        private ScopedActionListener _advanceListener;
        private bool _isActive;

        /// <param name="player"></param>
        /// <param name="holdToSkip"></param>
        /// <param name="data"></param>
        /// <param name="advanceAction"></param>
        /// <param name="onFinished">Runs once when the cutscene ends while this state is still active. Arg: skipped.</param>
        /// <param name="token"></param>
        public IntroCutsceneState(CutscenePlayer player, HoldToSkip holdToSkip, CutsceneData data,
                                  InputActionReference advanceAction, Action<bool> onFinished, CancellationToken token)
        {
            _player = player;
            _holdToSkip = holdToSkip;
            _data = data;
            _advanceAction = advanceAction;
            _onFinished = onFinished;
            _token = token;
        }

        public void Enter()
        {
            _isActive = true;
            _advanceListener = new ScopedActionListener(_advanceAction, OnAdvance);
            _holdToSkip.Activate(_player.RequestSkip);
            Run();
        }

        public void Exit()
        {
            _isActive = false;
            _advanceListener?.Dispose();
            _advanceListener = null;

            // NOTE: May run during scene unload, after the component is already destroyed.
            if (_holdToSkip)
                _holdToSkip.Deactivate();
        }

        // NOTE: The any-key action also fires for the skip key; those presses belong to hold-to-skip, not to advancing.
        private void OnAdvance(InputAction.CallbackContext _)
        {
            if (_isActive && !_holdToSkip.IsSkipKeyPressed)
                _player.RequestAdvance();
        }

        private async void Run()
        {
            try
            {
                var slides = _data ? _data.Slides : Array.Empty<CutsceneSlide>();
                var skipped = await _player.PlayAsync(slides, _token);
                if (!_isActive) return;

                GameEventsRelay.OnIntroCutsceneFinished.Invoke(skipped);
                _onFinished?.Invoke(skipped);
            }
            catch (OperationCanceledException)
            {
                // Scene unloaded mid-cutscene.
            }
        }
    }
}