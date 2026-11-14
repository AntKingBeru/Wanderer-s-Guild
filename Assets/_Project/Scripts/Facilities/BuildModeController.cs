// Build mode: toggle, catalog + ghost placement, and (with nothing to place) room selection with its upgrade/demolish card.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". LateUpdate runs after the camera rig (order 110), so the ghost
    // and room picking use this frame's final camera pose. The guided tutorial (TutorialGate) can keep it open, limit the
    // catalog to one room, pin the ghost to a spot and shorten the build.
    [DefaultExecutionOrder(110)]
    public class BuildModeController : Singleton<BuildModeController>
    {
        private const string RoomHint = "Click a room to manage it · B / Esc exit";
        private const string PlaceHint = "R rotate · Right-click cancel · Esc back";

        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldViewPresenter view;
        [SerializeField] private GamePanelHost panels;
        [SerializeField] private Grid grid;
        [Tooltip("The BuildMode UIDocument.")]
        [SerializeField] private UIDocument document;

        private readonly List<BuildCatalogEntry> _catalog = new();
        private ScopedActionListener _toggleListener, _rotateListener, _cancelListener, _escapeListener;
        private InputAction _point, _click, _wheel;
        private BuildModeView _view;
        private RoomCardView _card;
        private BuildGhost _ghost;
        private BuildGridOverlay _gridOverlay;
        private FacilityData _selected;
        private PlacedFacility _hoverRoom;
        private PlacedFacility _selectedRoom;
        private int _rotation;
        private Vector2Int _origin;
        private int _ghostRotation;
        private int _escapeFrame = -1;

        public bool IsActive { get; private set; }

        // True while build mode is open or used Esc this frame — the pause menu ignores that Esc.
        public bool BlocksEscape => IsActive || _escapeFrame == Time.frameCount;

        // The facility being placed (null = none) and whether a confirm is open — the tutorial follows both.
        public FacilityData Selected => _selected;
        public bool IsConfirming => _view is { IsConfirming: true };

        private static FacilityManager Facilities => FacilityManager.Instance;
        private static BuildModeSettings Settings => GameConfig.Instance.BuildMode;
        private static long Now => RequestManager.NowMinutes;

        private bool CanEnter =>
            FacilityManager.HasInstance && panels != null && !panels.HasOpenPanel && !panels.IsBusy &&
            !(SceneLoader.HasInstance && SceneLoader.Instance.IsBusy);

        private void OnEnable()
        {
            var bindings = GameConfig.Instance.InputBindings;
            _toggleListener = new ScopedActionListener(bindings.BuildMode, _ => Toggle());
            _rotateListener = new ScopedActionListener(bindings.BuildRotate, _ => Rotate());
            _cancelListener = new ScopedActionListener(bindings.BuildCancel, _ => StepBack(allowExit: false));
            _escapeListener = new ScopedActionListener(bindings.PauseMenu, _ => OnEscape());
            _point = Enable(bindings.Point);
            _click = Enable(bindings.WorldClick);
            _wheel = Enable(bindings.CameraZoom);

            GameEventsRelay.OnMenuPanelOpened.AddListener(OnPanelOpened);
            GameEventsRelay.OnGoldChanged.AddListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.AddListener(OnRankUp);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnFacilityPlaced.AddListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityCompleted.AddListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityUpgradeStarted.AddListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityRemoved);
        }

        private void OnDisable()
        {
            _toggleListener?.Dispose();
            _rotateListener?.Dispose();
            _cancelListener?.Dispose();
            _escapeListener?.Dispose();

            GameEventsRelay.OnMenuPanelOpened.RemoveListener(OnPanelOpened);
            GameEventsRelay.OnGoldChanged.RemoveListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.RemoveListener(OnRankUp);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnFacilityPlaced.RemoveListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityUpgradeStarted.RemoveListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityRemoved);
        }

        private void Start()
        {
            var root = document != null ? document.rootVisualElement : null;
            _view = BuildModeView.Bind(root, Pick);
            _card = RoomCardView.Bind(root, AskToUpgrade, AskToDemolish, () => SelectRoom(null));
            if (_view == null || _card == null)
                Debug.LogError("[BuildModeController] Missing build-mode elements in BuildMode.uxml.", this);

            _ghost = new BuildGhost(grid, transform);
            _gridOverlay = new BuildGridOverlay(grid, transform);
        }

        protected override void OnDestroy()
        {
            _ghost?.Clear();
            _gridOverlay?.Destroy();
            base.OnDestroy();
        }

        // NOTE: The tutorial may keep build mode open (its building steps).
        public void Toggle()
        {
            if (!IsActive)
                Enter();
            else if (!TutorialGate.KeepsBuildMode)
                Exit();
        }

        public void Enter()
        {
            if (IsActive || !CanEnter || !TutorialGate.AllowsBuildModeEnter || _view == null || _card == null)
                return;

            IsActive = true;
            _rotation = 0;
            RefreshCatalog();
            RefreshHint();
            _view.Show();
            GameEventsRelay.OnBuildModeChanged.Invoke(true);
        }

        // NOTE: Clears both outlines (hover + selection) — no room stays outlined during normal play. Unconditional (a panel
        // opening closes build mode even under the tutorial).
        public void Exit()
        {
            if (!IsActive)
                return;

            if (_view.IsConfirming)
                _view.CancelConfirm();
            DeselectFacility();
            SelectRoom(null);
            SetHoverRoom(null);
            IsActive = false;
            _view.Hide();
            _card.Hide();
            _gridOverlay?.Hide();
            GameEventsRelay.OnBuildModeChanged.Invoke(false);
        }

        // The catalog card showing this facility (null if none) — the tutorial spotlights it.
        public VisualElement CatalogCard(FacilityData data) => _view?.CardFor(data);

        // Scrolls the catalog so this facility's card is fully in view.
        public void RevealCard(FacilityData data) => _view?.RevealCard(data);

        // ---------- Frame ----------

        // NOTE: Frozen while a confirm is open, so the dialog always matches what's on screen. A tutorial-pinned room is
        // handled before the over-UI check, so its ghost stays put whatever the pointer is over.
        private void LateUpdate()
        {
            if (!IsActive || _view == null)
                return;

            var screen = _point.ReadValue<Vector2>();
            ScrollCatalog(screen);
            if (_view.IsConfirming)
                return;

            if (_selected != null && TutorialGate.TryGetPlacement(_selected, out var pinned))
            {
                UpdatePinnedGhost(pinned, screen);
                return;
            }

            if (view.IsPointerOverUI(screen) ||
                !BuildPointer.TryFloorPoint(worldCamera, view, screen, Settings.FloorHeight, out var floor, out var pixel))
            {
                HidePointerVisuals();
                return;
            }

            var cell = BuildPointer.CellAt(grid, floor);
            _gridOverlay.Show(cell, Settings.FloorHeight);

            if (_selected != null)
                UpdateGhost(cell, pixel);
            else
                UpdateRoomPicking(cell);
        }

        // NOTE: The wheel is read from the Input System — over the catalog it scrolls one card per notch (sign only; the
        // camera ignores the wheel over UI). Wheel down = right.
        private void ScrollCatalog(Vector2 screen)
        {
            if (_wheel == null)
                return;

            var value = _wheel.ReadValue<float>();
            if (Mathf.Abs(value) <= 0.01f || !_view.IsOverCatalog(screen))
                return;

            _view.ScrollCards(value < 0f ? 1 : -1);
        }

        private void UpdateGhost(Vector2Int cell, Vector2 pixel)
        {
            var rotated = FacilityFootprint.RotatedSize(_selected.Size, _rotation);
            var freeOrigin = cell - new Vector2Int(rotated.x / 2, rotated.y / 2);
            (_origin, _ghostRotation) = BuildSnapper.Snap(_selected, freeOrigin, _rotation, Facilities, Settings, GameConfig.Instance.Facilities);

            var check = Facilities.CanPlace(_selected, _origin, _ghostRotation);
            _ghost.Show(_selected, _origin, _ghostRotation, check.Allowed);
            _view.ShowCursor(PlacementLabel(check), check.Allowed, view.ViewToLocal(pixel + Settings.CursorLabelOffset));

            if (check.Allowed && _click != null && _click.WasPressedThisFrame())
                AskToBuild(check.Gold);
        }

        // NOTE: The tutorial pins the ghost to its spot; only a click on one of the spot's own cells (not on UI) asks to build.
        private void UpdatePinnedGhost(TutorialPlacement pinned, Vector2 screen)
        {
            _origin = pinned.Origin;
            _ghostRotation = pinned.Rotation;
            _gridOverlay.Hide();
            SetHoverRoom(null);

            var check = Facilities.CanPlace(_selected, _origin, _ghostRotation);
            _ghost.Show(_selected, _origin, _ghostRotation, check.Allowed);

            if (view.IsPointerOverUI(screen) ||
                !BuildPointer.TryFloorPoint(worldCamera, view, screen, Settings.FloorHeight, out var floor, out var pixel) ||
                !FacilityFootprint.Cells(_origin, _selected.Size, _ghostRotation).Contains(BuildPointer.CellAt(grid, floor)))
            {
                _view.HideCursor();
                return;
            }

            _view.ShowCursor(PlacementLabel(check), check.Allowed, view.ViewToLocal(pixel + Settings.CursorLabelOffset));
            if (check.Allowed && _click != null && _click.WasPressedThisFrame())
                AskToBuild(check.Gold);
        }

        // "150g · 6 hours" (the tutorial's shortened time), or why it can't go there.
        private string PlacementLabel(FacilityCheck check) =>
            check.Allowed ? $"{check.Gold:N0}g · {FacilityFormatter.BuildTime(_selected.BuildDaysFor(1), TutorialGate.BuildTimeScale)}"
                          : check.Reason;

        // NOTE: The floor cell under the pointer tells which room owns it (no colliders needed). Clicking empty floor
        // deselects the room. Off while the tutorial runs.
        private void UpdateRoomPicking(Vector2Int cell)
        {
            _view.HideCursor();
            if (!TutorialGate.AllowsRoomPicking)
            {
                SetHoverRoom(null);
                return;
            }

            var room = Facilities.RoomAt(cell);
            SetHoverRoom(room);

            if (_click != null && _click.WasPressedThisFrame())
                SelectRoom(room);
        }

        private void HidePointerVisuals()
        {
            _ghost?.Hide();
            _gridOverlay?.Hide();
            _view?.HideCursor();
            SetHoverRoom(null);
        }

        // ---------- Placing ----------

        // NOTE: While the tutorial runs, only its pinned room can be picked.
        private void Pick(FacilityData data)
        {
            if (!IsActive || _view.IsConfirming || !TutorialGate.AllowsFacility(data))
                return;

            if (data == _selected)
            {
                DeselectFacility();
                return;
            }

            SelectRoom(null);
            SetHoverRoom(null);
            _selected = data;
            _ghost.Build(data);
            RefreshCatalog();
            RefreshHint();
        }

        private void DeselectFacility()
        {
            if (_selected == null)
                return;

            _selected = null;
            _ghost?.Clear();
            _view?.HideCursor();
            RefreshCatalog();
            RefreshHint();
        }

        private void Rotate()
        {
            if (IsActive && _selected != null && !_view.IsConfirming)
                _rotation = (_rotation + 1) & 3;
        }

        // NOTE: TryPlace re-checks on Yes — anything that changed meanwhile simply refuses. The facility stays selected. The
        // build-time scale (the tutorial's) is taken now, so the dialog and the build agree.
        private void AskToBuild(long cost)
        {
            var data = _selected;
            var origin = _origin;
            var rotation = _ghostRotation;
            var scale = TutorialGate.BuildTimeScale;

            _view.Confirm($"Build {data.DisplayName}?\n{cost:N0}g · {FacilityFormatter.BuildTime(data.BuildDaysFor(1), scale)}",
                          onYes: () => Facilities.TryPlace(data, origin, rotation, scale),
                          onClosed: null);
        }

        // ---------- Rooms ----------

        // NOTE: A room is outlined while hovered OR selected — each change only touches the room(s) involved.
        private void SetHoverRoom(PlacedFacility room)
        {
            if (room == _hoverRoom)
                return;

            var previous = _hoverRoom;
            _hoverRoom = room;
            RefreshOutline(previous);
            RefreshOutline(_hoverRoom);
        }

        private void SelectRoom(PlacedFacility room)
        {
            if (room == _selectedRoom)
            {
                RefreshCard();
                return;
            }

            var previous = _selectedRoom;
            _selectedRoom = room;
            RefreshOutline(previous);
            RefreshOutline(_selectedRoom);
            RefreshCard();
        }

        private void RefreshOutline(PlacedFacility room)
        {
            if (room != null && FacilityManager.HasInstance)
                Facilities.SetHighlighted(room, room == _hoverRoom || room == _selectedRoom);
        }

        private void RefreshCard()
        {
            if (_card == null)
                return;
            if (_selectedRoom == null || !IsActive)
            {
                _card.Hide();
                return;
            }

            _card.Show(_selectedRoom, Facilities.CanUpgrade(_selectedRoom), Facilities.CanDemolish(_selectedRoom), Now);
        }

        // NOTE: TryUpgrade / TryDemolish re-check on Yes. After a demolish the card closes (the room's removal event).
        private void AskToUpgrade()
        {
            var room = _selectedRoom;
            if (room == null || _view.IsConfirming || !Facilities.CanUpgrade(room).Allowed)
                return;

            var next = room.Level + 1;
            _view.Confirm($"Upgrade {room.Data.DisplayName} to Lv {next}?\n{room.Data.CostFor(next):N0}g · {FacilityFormatter.Days(room.Data.BuildDaysFor(next))}",
                          onYes: () => Facilities.TryUpgrade(room),
                          onClosed: RefreshCard);
        }

        private void AskToDemolish()
        {
            var room = _selectedRoom;
            if (room == null || _view.IsConfirming)
                return;

            var check = Facilities.CanDemolish(room);
            if (!check.Allowed)
                return;

            _view.Confirm($"Demolish {room.Data.DisplayName}?\n+{check.Gold:N0}g refund",
                          onYes: () => Facilities.TryDemolish(room),
                          onClosed: RefreshCard);
        }

        // ---------- Stepping back ----------

        // NOTE: Esc steps back: confirm → placing → room → build mode. The frame is recorded so the pause menu ignores it.
        private void OnEscape()
        {
            if (!IsActive)
                return;
            _escapeFrame = Time.frameCount;
            StepBack(allowExit: true);
        }

        // NOTE: The last step (leaving build mode) is skipped while the tutorial keeps it opened.
        private void StepBack(bool allowExit)
        {
            if (!IsActive) return;

            if (_view.IsConfirming)
                _view.CancelConfirm();
            else if (_selected != null)
                DeselectFacility();
            else if (_selectedRoom != null)
                SelectRoom(null);
            else if (allowExit && !TutorialGate.KeepsBuildMode)
                Exit();
        }

        // ---------- Catalog, hint, events ----------

        // NOTE: Guild Hall excluded; ordered by unlock rank, then cost. Rank/limit lock a card; gold only colors it.
        private void RefreshCatalog()
        {
            if (!IsActive || _view == null || !FacilityManager.HasInstance)
                return;

            var rank = GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GuildRank.F;
            var gold = GuildManager.HasInstance ? GuildManager.Instance.Wallet.Gold : 0;
            var hallId = GameConfig.Instance.Facilities.GuildHallId;

            _catalog.Clear();
            foreach (var data in Facilities.Catalog.Where(d => d.Id != hallId)
                         .OrderBy(d => d.UnlockRankFor(1)).ThenBy(d => d.CostFor(1)))
            {
                var reason = FacilityRules.LockReason(data, Facilities.Rooms, rank);
                _catalog.Add(new BuildCatalogEntry(data, reason, gold >= data.CostFor(1)));
            }

            _view.ShowCatalog(_catalog, _selected);
        }

        private void RefreshHint() => _view?.SetHint(_selected ? PlaceHint : RoomHint);

        private void OnPanelOpened(MenuPanelId _) => Exit();

        private void OnGoldChanged(long _, long __)
        {
            RefreshCatalog();
            RefreshCard();
        }

        private void OnRankUp(GuildRank _)
        {
            RefreshCatalog();
            RefreshCard();
        }

        private void OnHourStarted(GameDateTime _) => RefreshCard();

        private void OnFacilityChanged(PlacedFacility _)
        {
            RefreshCatalog();
            RefreshCard();
        }

        // NOTE: Raised before the room's views are destroyed — drop it from hover/selection first.
        private void OnFacilityRemoved(PlacedFacility room)
        {
            if (room == _hoverRoom)
                SetHoverRoom(null);
            if (room == _selectedRoom)
                SelectRoom(null);
            RefreshCatalog();
            RefreshCard();
        }

        private static InputAction Enable(InputActionReference reference)
        {
            var action = reference != null ? reference.action : null;
            action?.Enable();
            return action;
        }
    }
}