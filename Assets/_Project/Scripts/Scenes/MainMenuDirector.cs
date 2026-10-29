// Drives the MainMenu scene flow (title screen → camera pan → menu, or straight to the menu when returning from a game).
using System.Threading;
using UnityEngine;

namespace WanderersGuild
{
    public class MainMenuDirector : MonoBehaviour
    {
        [SerializeField] private CameraPanner cameraPanner;
        [SerializeField] private Transform titleAnchor;
        [SerializeField] private Transform menuAnchor;
        [SerializeField] private TitleScreenView titleScreenView;

        [Tooltip("Optional: the title character's click router. Clicks on the character then don't dismiss the title.")]
        [SerializeField] private TitleCharacterClickRouter characterClicks;

        private readonly StateMachine _stateMachine = new();
        private IState _startState;
        private PanToMenuState _panState;
        private MainMenuState _menuState;

        // NOTE: Context accessors the states use; states never touch the scene directly.
        public CameraPanner CameraPanner => cameraPanner;
        public Transform MenuAnchor => menuAnchor;
        public TitleScreenView TitleScreenView => titleScreenView;
        public CancellationToken LifetimeToken => destroyCancellationToken;

        // Null when no character is set up (converts Unity's "fake null" for an unassigned field).
        public TitleCharacterClickRouter CharacterClicks => characterClicks ? characterClicks : null;

        private void Awake()
        {
            if (!cameraPanner || !titleAnchor || !menuAnchor || !titleScreenView)
            {
                Debug.LogError("[MainMenuDirector] Missing scene references.", this);
                enabled = false; // Prevents Start; OnDestroy still runs safely.
                return;
            }

            _panState = new PanToMenuState(this);
            _menuState = new MainMenuState();

            // NOTE: Returning from a game skips the title screen; the camera snaps while the screen is still black.
            var returning = MainMenuEntry.ConsumeSkipTitle();
            _startState = returning ? new MenuReturnState(this) : new TitleScreenState(this);
            cameraPanner.SnapTo(returning ? menuAnchor : titleAnchor);
        }

        private void Start() => _stateMachine.ChangeState(_startState);

        private void OnDestroy() => _stateMachine.Shutdown(); // Releases input and event listeners.

        public void EnterPanState() => _stateMachine.ChangeState(_panState);
        public void EnterMenuState() => _stateMachine.ChangeState(_menuState);
    }
}