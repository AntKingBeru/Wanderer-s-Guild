// Build mode: toggle (B / HUD), catalog, ghost that follows and snaps, R rotate, click to build with a confirm, Esc steps back.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". LateUpdate runs after the camera rig (order 110), so the ghost
    // follows this frame's final camera pose.
    [DefaultExecutionOrder(110)]
    public class BuildModeController : Singleton<BuildModeController>
    {
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
        private BuildGhost _ghost;
        private BuildGridOverlay _gridOverlay;
        private FacilityData _selected;
        private int _rotation;
        private Vector2Int _origin;
        private int _ghostRotation;
        private int _escapeFrame = -1;

        public bool IsActive { get; private set; }

        // True while build mode is open or used Esc this frame — the pause menu ignores that Esc.
        public bool BlocksEscape => IsActive || _escapeFrame == Time.frameCount;

        private static FacilityManager Facilities => FacilityManager.Instance;
        private static BuildModeSettings Settings => GameConfig.Instance.BuildMode;

        private bool CanEnter =>
            FacilityManager.HasInstance && panels && !panels.HasOpenPanel && !panels.IsBusy &&
            !(SceneLoader.HasInstance && SceneLoader.Instance.IsBusy);

        private void OnEnable()
        {
            var bindings = GameConfig.Instance.InputBindings;
            _toggleListener = new ScopedActionListener(bindings.BuildMode, _ => Toggle());
            _rotateListener = new ScopedActionListener(bindings.BuildRotate, _ => Rotate());
            _cancelListener = new ScopedActionListener(bindings.BuildCancel, _ => Deselect());
            _escapeListener = new ScopedActionListener(bindings.PauseMenu, _ => OnEscape());
            _point = Enable(bindings.Point);
            _click = Enable(bindings.WorldClick);
            _wheel = Enable(bindings.CameraZoom);

            GameEventsRelay.OnMenuPanelOpened.AddListener(OnPanelOpened);
            GameEventsRelay.OnGoldChanged.AddListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.AddListener(OnRankUp);
            GameEventsRelay.OnFacilityPlaced.AddListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilitiesChanged);
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
            GameEventsRelay.OnFacilityPlaced.RemoveListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilitiesChanged);
        }

        private void Start()
        {
            _view = document ? BuildModeView.Bind(document.rootVisualElement, Pick) : null;
            if (_view == null)
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

        public void Toggle()
        {
            if (IsActive) Exit();
            else Enter();
        }

        public void Enter()
        {
            if (IsActive || !CanEnter || _view == null)
                return;

            IsActive = true;
            _rotation = 0;
            RefreshCatalog();
            _view.Show();
            GameEventsRelay.OnBuildModeChanged.Invoke(true);
        }

        public void Exit()
        {
            if (!IsActive)
                return;

            Deselect();
            IsActive = false;
            _view?.Hide();
            _gridOverlay?.Hide();
            GameEventsRelay.OnBuildModeChanged.Invoke(false);
        }

        // ---------- Frame ----------

        // NOTE: Frozen while the confirm is open, so the dialog always matches the ghost on screen.
        private void LateUpdate()
        {
            if (!IsActive || _view == null)
                return;

            var screen = _point.ReadValue<Vector2>();
            ScrollCatalog(screen);
            if (_view.IsConfirming)
                return;

            if (view.IsPointerOverUI(screen) ||
                !BuildPointer.TryFloorPoint(worldCamera, view, screen, Settings.FloorHeight, out var floor, out var pixel))
            {
                HidePointerVisuals();
                return;
            }

            var cell = BuildPointer.CellAt(grid, floor);
            _gridOverlay.Show(cell, Settings.FloorHeight);
            if (!_selected)
            {
                _view.HideCursor();
                return;
            }

            UpdateGhost(cell, pixel);
        }

        // NOTE: The wheel is read from the Input System (UI Toolkit doesn't receive wheel events here) — over the catalog it
        // scrolls one card per notch (sign only; the camera ignores the wheel over UI). Wheel down = right.
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
            Vector2Int rotated = FacilityFootprint.RotatedSize(_selected.Size, _rotation);
            Vector2Int freeOrigin = cell - new Vector2Int(rotated.x / 2, rotated.y / 2);
            (_origin, _ghostRotation) = BuildSnapper.Snap(_selected, freeOrigin, _rotation, Facilities, Settings, GameConfig.Instance.Facilities);

            FacilityCheck check = Facilities.CanPlace(_selected, _origin, _ghostRotation);
            _ghost.Show(_selected, _origin, _ghostRotation, check.Allowed);

            string label = check.Allowed ? $"{check.Gold:N0}g · {Days(_selected.BuildDaysFor(1))}" : check.Reason;
            _view.ShowCursor(label, check.Allowed, view.ViewToLocal(pixel + Settings.CursorLabelOffset));

            if (check.Allowed && _click != null && _click.WasPressedThisFrame())
                AskToBuild(check.Gold);
        }

        private void HidePointerVisuals()
        {
            _ghost?.Hide();
            _gridOverlay?.Hide();
            _view?.HideCursor();
        }

        // ---------- Actions ----------

        private void Pick(FacilityData data)
        {
            if (!IsActive || _view.IsConfirming) return;

            if (data == _selected) Deselect();
            else Select(data);
        }

        private void Select(FacilityData data)
        {
            _selected = data;
            _ghost.Build(data);
            RefreshCatalog();
        }

        private void Deselect()
        {
            if (_view is { IsConfirming: true })
            {
                _view.CancelConfirm();
                return;
            }

            if (!_selected)
                return;
            _selected = null;
            _ghost?.Clear();
            _view?.HideCursor();
            RefreshCatalog();
        }

        private void Rotate()
        {
            if (IsActive && _selected && !_view.IsConfirming)
                _rotation = (_rotation + 1) & 3;
        }

        // NOTE: Esc steps back: confirm → selection → build mode. The frame is recorded so the pause menu ignores this Esc.
        private void OnEscape()
        {
            if (!IsActive)
                return;
            _escapeFrame = Time.frameCount;

            if (_view.IsConfirming)
                _view.CancelConfirm();
            else if (_selected)
                Deselect();
            else
                Exit();
        }

        // NOTE: TryPlace re-checks on Yes — anything that changed meanwhile simply refuses. The facility stays selected.
        private void AskToBuild(long cost)
        {
            var data = _selected;
            var origin = _origin;
            var rotation = _ghostRotation;

            _view.Confirm($"Build {data.DisplayName}?\n{cost:N0}g · {Days(data.BuildDaysFor(1))}",
                          onYes: () => Facilities.TryPlace(data, origin, rotation),
                          onClosed: null);
        }

        // ---------- Catalog ----------

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

        private void OnPanelOpened(MenuPanelId _) => Exit();
        private void OnGoldChanged(long _, long __) => RefreshCatalog();
        private void OnRankUp(GuildRank _) => RefreshCatalog();
        private void OnFacilitiesChanged(PlacedFacility _) => RefreshCatalog();

        private static string Days(int days) => days == 1 ? "1 day" : $"{days} days";

        private static InputAction Enable(InputActionReference reference)
        {
            var action = reference ? reference.action : null;
            action?.Enable();
            return action;
        }
    }
}