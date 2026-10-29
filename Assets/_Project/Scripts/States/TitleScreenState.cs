// Title state: waits for the scene fade-in, then for any key or click to start the camera pan (clicks on the character excepted).
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    public sealed class TitleScreenState : IState
    {
        private readonly MainMenuDirector _director;
        private ScopedActionListener _anyKeyListener;

        public TitleScreenState(MainMenuDirector director) => _director = director;

        public void Enter()
        {
            _director.TitleScreenView.SetPromptOpacityImmediate(1f);

            // NOTE: Input is armed only once the screen is visible, so a press during the fade-in is ignored.
            if (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy)
                GameEventsRelay.OnSceneTransitionCompleted.AddListener(OnTransitionCompleted);
            else
                ArmInput();
        }

        public void Exit()
        {
            GameEventsRelay.OnSceneTransitionCompleted.RemoveListener(OnTransitionCompleted);
            _anyKeyListener?.Dispose();
            _anyKeyListener = null;
        }

        private void OnTransitionCompleted(SceneId _)
        {
            GameEventsRelay.OnSceneTransitionCompleted.RemoveListener(OnTransitionCompleted);
            ArmInput();
        }

        private void ArmInput() =>
            _anyKeyListener ??= new ScopedActionListener(GameConfig.Instance.InputBindings.AnyKey, OnAnyKey);

        // NOTE: A click on the character (face, eyes, eyebrows, moustache) pokes it instead of starting the game.
        private void OnAnyKey(InputAction.CallbackContext context)
        {
            TitleCharacterClickRouter characterClicks = _director.CharacterClicks;
            if (characterClicks && characterClicks.TryHandleClick(context))
                return;

            _director.EnterPanState();
        }
    }
}