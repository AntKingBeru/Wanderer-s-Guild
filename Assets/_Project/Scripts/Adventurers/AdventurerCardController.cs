// Shows the floating card for a clicked adventurer, keeps it beside them, and closes it on click-away, Esc, panels or leaving.
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    // NOTE: After the interaction controller (100) and the avatars' billboards (120), so it sees this frame's click and pose.
    [DefaultExecutionOrder(130)]
    public class AdventurerCardController : Singleton<AdventurerCardController>
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldViewPresenter view;
        [SerializeField] private GamePanelHost panels;

        private ScopedActionListener _escapeListener;
        private InputAction _point, _click;
        private AdventurerCardView _card;
        private int _adventurerId = -1;
        private int _openedFrame = -1;
        private int _escapeFrame = -1;

        public bool IsOpen => _adventurerId >= 0;

        // True while the card is open or Esc closed it this frame — the pause menu ignores that Esc.
        public bool BlocksEscape => IsOpen || _escapeFrame == Time.frameCount;

        private void OnEnable()
        {
            var bindings = GameConfig.Instance.InputBindings;
            _escapeListener = new ScopedActionListener(bindings.PauseMenu, _ => OnEscape());
            _point = Enable(bindings.Point);
            _click = Enable(bindings.WorldClick);

            GameEventsRelay.OnAdventurerCardRequested.AddListener(Open);
            GameEventsRelay.OnAdventurerChanged.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerLeveledUp.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnMenuPanelOpened.AddListener(OnPanelOpened);
            GameEventsRelay.OnBuildModeChanged.AddListener(OnBuildModeChanged);
        }

        private void OnDisable()
        {
            _escapeListener?.Dispose();
            GameEventsRelay.OnAdventurerCardRequested.RemoveListener(Open);
            GameEventsRelay.OnAdventurerChanged.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerLeveledUp.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnMenuPanelOpened.RemoveListener(OnPanelOpened);
            GameEventsRelay.OnBuildModeChanged.RemoveListener(OnBuildModeChanged);
        }

        private void Start()
        {
            _card = view ? AdventurerCardView.Bind(view.DocumentRoot, OpenDetails, Close) : null;
            if (_card == null)
                Debug.LogError("[AdventurerCardController] Missing 'world-card' elements in WorldView.uxml.", this);
        }

        // Shows (or moves) the card to this adventurer.
        public void Open(int adventurerId)
        {
            if (_card == null || !AdventurerManager.HasInstance)
                return;

            var adventurer = AdventurerManager.Instance.Find(adventurerId);
            if (adventurer == null)
                return;

            _adventurerId = adventurerId;
            _openedFrame = Time.frameCount;
            _card.SetContent(adventurer, RequestManager.NowMinutes);
            _card.Show();
            Follow();
        }

        public void Close()
        {
            _adventurerId = -1;
            _card?.Hide();
        }

        // NOTE: A click this frame that opened the card is ignored; any other press outside UI closes it (another avatar's
        // click re-targets it first, through the event).
        private void LateUpdate()
        {
            if (!IsOpen)
                return;

            if (!AvatarManager.HasInstance || !AvatarManager.Instance.TryGet(_adventurerId, out _) || _click != null
                && _click.WasPressedThisFrame() && Time.frameCount != _openedFrame &&
                !view.IsPointerOverUI(_point.ReadValue<Vector2>()))
            {
                Close();
                return;
            }

            Follow();
        }

        private void Follow()
        {
            if (!AvatarManager.Instance.TryGet(_adventurerId, out AdventurerAvatar avatar) ||
                !avatar.TryGetComponent(out AvatarInteractable interactable))
                return;

            var viewport = worldCamera.WorldToViewportPoint(interactable.LabelAnchor);
            if (viewport.z <= 0f)
            {
                _card.Hide();
                return;
            }

            var settings = GameConfig.Instance.Avatars;
            var size = view.RenderSize;
            var pixel = new Vector2(viewport.x * size.x, (1f - viewport.y) * size.y) + settings.CardOffset;
            var local = view.ViewToLocal(pixel);
            Rect area = new(Vector2.zero, view.ViewToLocal(size));

            _card.Show();
            _card.Place(local, area, view.ViewToLocal(new Vector2(0f, settings.CardTopMargin)).y);
        }

        // NOTE: Opens the roster with this adventurer selected (one-shot), then closes the card.
        private void OpenDetails()
        {
            if (!IsOpen || !panels || panels.IsBusy || panels.HasOpenPanel)
                return;

            var id = _adventurerId;
            Close();
            RosterPanel.SelectOnNextOpen(id);
            panels.Open(MenuPanelId.Roster);
        }

        private void OnEscape()
        {
            if (!IsOpen)
                return;
            _escapeFrame = Time.frameCount;
            Close();
        }

        private void OnAdventurerEvent(Adventurer adventurer)
        {
            if (IsOpen && adventurer.Id == _adventurerId) _card.SetContent(adventurer, RequestManager.NowMinutes);
        }

        private void OnHourStarted(GameDateTime _)
        {
            if (!IsOpen || !AdventurerManager.HasInstance)
                return;

            var adventurer = AdventurerManager.Instance.Find(_adventurerId);
            if (adventurer != null)
                _card.SetContent(adventurer, RequestManager.NowMinutes);
        }

        private void OnPanelOpened(MenuPanelId _) => Close();

        private void OnBuildModeChanged(bool active)
        {
            if (active)
                Close();
        }

        private static InputAction Enable(InputActionReference reference)
        {
            var action = reference ? reference.action : null;
            action?.Enable();
            return action;
        }
    }
}