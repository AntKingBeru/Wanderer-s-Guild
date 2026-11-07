// Shows a bedroom's card (beds, residents) above it, outlines the room and its residents, and closes it like the adventurer card.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    // NOTE: After the interaction controller (100) and billboards (120), like the adventurer card (130).
    [DefaultExecutionOrder(131)]
    public class RoomLodgingCardController : Singleton<RoomLodgingCardController>
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldViewPresenter view;
        [SerializeField] private Grid grid;

        private readonly List<int> _residentIds = new();
        private readonly List<string> _names = new();
        private readonly List<AvatarInteractable> _selected = new();
        private ScopedActionListener _escapeListener;
        private InputAction _point, _click;
        private RoomLodgingCardView _card;
        private BedroomTarget _anchor;
        private PlacedFacility _room;
        private int _openedFrame = -1;
        private int _escapeFrame = -1;

        public bool IsOpen => _room != null;
        public bool BlocksEscape => IsOpen || _escapeFrame == Time.frameCount;

        private void OnEnable()
        {
            var bindings = GameConfig.Instance.InputBindings;
            _escapeListener = new ScopedActionListener(bindings.PauseMenu, _ => OnEscape());
            _point = Enable(bindings.Point);
            _click = Enable(bindings.WorldClick);

            GameEventsRelay.OnRoomCardRequested.AddListener(Open);
            GameEventsRelay.OnAdventurerCardRequested.AddListener(OnAdventurerCard);
            GameEventsRelay.OnLodgingChanged.AddListener(OnLodgingChanged);
            GameEventsRelay.OnAdventurerLeft.AddListener(OnAdventurerLeft);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityRemoved);
            GameEventsRelay.OnFacilityCompleted.AddListener(OnFacilityCompleted);
            GameEventsRelay.OnMenuPanelOpened.AddListener(OnPanelOpened);
            GameEventsRelay.OnBuildModeChanged.AddListener(OnBuildModeChanged);
        }

        private void OnDisable()
        {
            _escapeListener?.Dispose();
            GameEventsRelay.OnRoomCardRequested.RemoveListener(Open);
            GameEventsRelay.OnAdventurerCardRequested.RemoveListener(OnAdventurerCard);
            GameEventsRelay.OnLodgingChanged.RemoveListener(OnLodgingChanged);
            GameEventsRelay.OnAdventurerLeft.RemoveListener(OnAdventurerLeft);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityRemoved);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(OnFacilityCompleted);
            GameEventsRelay.OnMenuPanelOpened.RemoveListener(OnPanelOpened);
            GameEventsRelay.OnBuildModeChanged.RemoveListener(OnBuildModeChanged);
            Close();
        }

        private void Start()
        {
            _card = view ? RoomLodgingCardView.Bind(view.DocumentRoot, Close) : null;
            if (_card == null)
                Debug.LogError("[RoomLodgingCardController] Missing 'room-card' elements in WorldView.uxml.", this);
        }

        public void Open(int roomId)
        {
            if (_card == null || !FacilityManager.HasInstance)
                return;

            var room = LodgingRules.FindRoom(FacilityManager.Instance.Rooms, roomId);
            if (room == null)
                return;

            Close();
            _room = room;
            _anchor = new BedroomTarget(room, grid);
            _openedFrame = Time.frameCount;
            RoomHighlights.Add(room);
            Refresh();
            _card.Show();
            Follow();
        }

        public void Close()
        {
            ClearSelection();
            if (_room != null)
                RoomHighlights.Remove(_room);
            _room = null;
            _anchor = null;
            _card?.Hide();
        }

        private void LateUpdate()
        {
            if (!IsOpen)
                return;

            if (_click != null && _click.WasPressedThisFrame() && Time.frameCount != _openedFrame &&
                !view.IsPointerOverUI(_point.ReadValue<Vector2>()))
            {
                Close();
                return;
            }

            Follow();
        }

        // NOTE: Residents are re-read and their avatars re-outlined (someone may have walked in from the door, or left).
        private void Refresh()
        {
            ClearSelection();
            if (!LodgingManager.HasInstance || !AdventurerManager.HasInstance)
                return;

            LodgingManager.Instance.Assignments.ResidentsOf(_room.Id, _residentIds);
            _names.Clear();
            foreach (var id in _residentIds)
            {
                var resident = AdventurerManager.Instance.Find(id);
                if (resident == null)
                    continue;

                _names.Add(resident.Name);
                if (AvatarManager.HasInstance && AvatarManager.Instance.TryGet(id, out var avatar) &&
                    avatar.TryGetComponent(out AvatarInteractable interactable))
                {
                    interactable.SetSelected(true);
                    _selected.Add(interactable);
                }
            }

            var title = $"{LodgingRules.Label(_room, FacilityManager.Instance.Rooms)} · Lv {_room.Level}";
            _card.SetContent(title, _names.Count, LodgingRules.Beds(_room), _names);
        }

        private void ClearSelection()
        {
            foreach (var interactable in _selected.Where(interactable => interactable))
            {
                interactable.SetSelected(false);
            }

            _selected.Clear();
        }

        private void Follow()
        {
            var viewport = worldCamera.WorldToViewportPoint(_anchor.LabelAnchor);
            if (viewport.z <= 0f)
            {
                _card.Hide();
                return;
            }

            var size = view.RenderSize;
            var local = view.ViewToLocal(new Vector2(viewport.x * size.x, (1f - viewport.y) * size.y));
            Rect area = new(Vector2.zero, view.ViewToLocal(size));
            var top = view.ViewToLocal(new Vector2(0f, GameConfig.Instance.Avatars.CardTopMargin)).y;

            _card.Show();
            _card.Place(local, area, top);
        }

        private void OnEscape()
        {
            if (!IsOpen)
                return;
            _escapeFrame = Time.frameCount;
            Close();
        }

        private void OnAdventurerCard(int _) => Close();

        private void OnLodgingChanged(Adventurer _, int __, bool ___)
        {
            if (IsOpen)
                Refresh();
        }

        private void OnAdventurerLeft(Adventurer _)
        {
            if (IsOpen)
                Refresh();
        }

        private void OnFacilityCompleted(PlacedFacility room)
        {
            if (IsOpen && room == _room)
                Refresh();
        }

        private void OnFacilityRemoved(PlacedFacility room)
        {
            if (room != _room)
                return;
            RoomHighlights.Forget(room);
            _room = null;
            ClearSelection();
            _anchor = null;
            _card?.Hide();
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