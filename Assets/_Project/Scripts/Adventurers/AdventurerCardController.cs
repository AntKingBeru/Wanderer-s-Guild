// Shows the floating card for a clicked adventurer, keeps it beside them, outlines their room, and closes it on click-away,
// Esc, panels, a room card, or them leaving.
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
        private PlacedFacility _outlinedRoom;
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
            GameEventsRelay.OnLodgingChanged.AddListener(OnLodgingChanged);
            GameEventsRelay.OnRoomCardRequested.AddListener(OnRoomCardRequested);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityRemoved);
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
            GameEventsRelay.OnLodgingChanged.RemoveListener(OnLodgingChanged);
            GameEventsRelay.OnRoomCardRequested.RemoveListener(OnRoomCardRequested);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityRemoved);
            Close();
        }

        private void Start()
        {
            _card = view != null ? AdventurerCardView.Bind(view.DocumentRoot, OpenDetails, Close) : null;
            if (_card == null)
                Debug.LogError("[AdventurerCardController] Missing 'world-card' elements in WorldView.uxml.", this);
        }

        // Shows (or moves) the card to this adventurer and outlines their room.
        public void Open(int adventurerId)
        {
            if (_card == null || !AdventurerManager.HasInstance)
                return;

            var adventurer = AdventurerManager.Instance.Find(adventurerId);
            if (adventurer == null)
                return;

            _adventurerId = adventurerId;
            _openedFrame = Time.frameCount;
            RefreshContent(adventurer);
            _card.Show();
            Follow();
            OutlineRoomOf(adventurerId);
        }

        public void Close()
        {
            ClearRoomOutline();
            _adventurerId = -1;
            _card?.Hide();
        }

        // NOTE: A click this frame that opened the card is ignored; any other press outside UI closes it (another avatar's
        // click re-targets it first, through the event).
        private void LateUpdate()
        {
            if (!IsOpen) return;

            if (!AvatarManager.HasInstance || !AvatarManager.Instance.TryGet(_adventurerId, out _))
            {
                Close();
                return;
            }

            if (_click != null && _click.WasPressedThisFrame() && Time.frameCount != _openedFrame &&
                !view.IsPointerOverUI(_point.ReadValue<Vector2>()))
            {
                Close();
                return;
            }

            Follow();
        }

        private void RefreshContent(Adventurer adventurer) =>
            _card.SetContent(adventurer, RequestManager.NowMinutes, PartyName(adventurer), RoomLabel(adventurer));

        private void Follow()
        {
            if (!AvatarManager.Instance.TryGet(_adventurerId, out var avatar) ||
                !avatar.TryGetComponent(out AvatarInteractable interactable))
                return;

            var viewport = worldCamera.WorldToViewportPoint(interactable.LabelAnchor);
            if (viewport.z <= 0f)
            {
                _card.Hide();
                return;
            }

            var settings = GameConfig.Instance.Avatars;
            var scale = view.PixelScale;
            var size = view.RenderSize;
            var pixel = new Vector2(viewport.x * size.x, (1f - viewport.y) * size.y) + settings.CardOffset * scale;
            var local = view.ViewToLocal(pixel);
            Rect area = new(Vector2.zero, view.ViewToLocal(size));

            _card.Show();
            _card.Place(local, area, view.ViewToLocal(new Vector2(0f, settings.CardTopMargin * scale)).y);
        }

        // ---------- Room outline ----------

        // NOTE: Through RoomHighlights, so hovering and leaving the same room never clears this outline.
        private void OutlineRoomOf(int adventurerId)
        {
            ClearRoomOutline();
            if (!LodgingManager.HasInstance || !FacilityManager.HasInstance)
                return;

            var roomId = LodgingManager.Instance.RoomOf(adventurerId);
            _outlinedRoom = roomId > 0 ? LodgingRules.FindRoom(FacilityManager.Instance.Rooms, roomId) : null;
            if (_outlinedRoom != null)
                RoomHighlights.Add(_outlinedRoom);
        }

        private void ClearRoomOutline()
        {
            if (_outlinedRoom != null)
                RoomHighlights.Remove(_outlinedRoom);
            _outlinedRoom = null;
        }

        // ---------- Buttons / input ----------

        // NOTE: Opens the roster with this adventurer selected (one-shot), then closes the card.
        private void OpenDetails()
        {
            if (!IsOpen || panels == null || panels.IsBusy || panels.HasOpenPanel)
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

        // ---------- Events ----------

        private void OnAdventurerEvent(Adventurer adventurer)
        {
            if (IsOpen && adventurer.Id == _adventurerId) RefreshContent(adventurer);
        }

        private void OnHourStarted(GameDateTime _)
        {
            if (!IsOpen || !AdventurerManager.HasInstance)
                return;

            var adventurer = AdventurerManager.Instance.Find(_adventurerId);
            if (adventurer != null)
                RefreshContent(adventurer);
        }

        // NOTE: Their room changed (moved, re-housed): new text and the new room outlined.
        private void OnLodgingChanged(Adventurer adventurer, int roomId, bool fromOutside)
        {
            if (!IsOpen || adventurer.Id != _adventurerId)
                return;

            RefreshContent(adventurer);
            OutlineRoomOf(_adventurerId);
        }

        // NOTE: Only one world card at a time.
        private void OnRoomCardRequested(int roomId) => Close();

        // NOTE: The outlined room is gone — its views (and outline) went with it; just forget the count.
        private void OnFacilityRemoved(PlacedFacility room)
        {
            if (room != _outlinedRoom)
                return;

            RoomHighlights.Forget(room);
            _outlinedRoom = null;
        }

        private void OnPanelOpened(MenuPanelId _) => Close();

        private void OnBuildModeChanged(bool active)
        {
            if (active)
                Close();
        }

        private static string PartyName(Adventurer adventurer)
        {
            var party = PartyManager.HasInstance ? PartyManager.Instance.PartyOf(adventurer.Id) : null;
            return party != null ? party.Name : string.Empty;
        }

        private static string RoomLabel(Adventurer adventurer) =>
            LodgingManager.HasInstance ? $"Room: {LodgingManager.Instance.LabelFor(adventurer.Id)}" : string.Empty;

        private static InputAction Enable(InputActionReference reference)
        {
            var action = reference != null ? reference.action : null;
            action?.Enable();
            return action;
        }
    }
}