// Game-scene owner of the guild's rooms: Guild Hall setup, place / upgrade / demolish, construction over days, save/load.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Awake runs after SaveGameService (−50) has loaded.
    public class FacilityManager : Singleton<FacilityManager>, ISaveContributor
    {
        [Tooltip("Unity Grid with Cell Size 1 and Cell Swizzle XZY (grid y = world z).")]
        [SerializeField] private Grid grid;
        [Tooltip("Spawned room models and scaffolds go under this.")]
        [SerializeField] private Transform roomsParent;
        [Tooltip("Resized to cover every room (the camera's bounds).")]
        [SerializeField] private CameraBoundsArea cameraBounds;

        private readonly List<PlacedFacility> _rooms = new();
        private FacilitySettings _settings;
        private Dictionary<string, FacilityData> _facilities = new();
        private FacilityGrid _grid;
        private FacilityViews _views;
        private int _nextId = 1;
        private int _hallId = -1;

        public IReadOnlyList<PlacedFacility> Rooms => _rooms;
        public FacilityGrid Grid => _grid;
        public bool HasGuildHall => _hallId >= 0;

        private static long Now => RequestManager.NowMinutes;
        private static GuildRank GuildRank => GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GameConfig.Instance.NewGuild.StartingRank;
        private static long Gold => GuildManager.HasInstance ? GuildManager.Instance.Wallet.Gold : 0L;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Facilities;
            if (_settings.Database)
                _facilities = _settings.Database.BuildLookup();
            if (_facilities.Count == 0)
                Debug.LogWarning("[FacilityManager] Facility database is empty or unassigned (run Database › Load).", this);

            _grid = new FacilityGrid();
            _views = new FacilityViews(grid, roomsParent ? roomsParent : transform, _settings);
            Restore(SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null);
        }

        private void OnEnable() => GameEventsRelay.OnTimeTick.AddListener(OnTimeTick);
        private void OnDisable() => GameEventsRelay.OnTimeTick.RemoveListener(OnTimeTick);

        // NOTE: Every load checks for a Guild Hall — a new guild, an older save, or a save written while the hall couldn't
        // be placed (e.g. the database wasn't assigned yet) all get one. Rooms can only exist connected to the hall, so its
        // cells are free whenever it's missing.
        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);

            if (!HasGuildHall)
                PlaceGuildHall();

            foreach (var room in _rooms)
                _views.Refresh(room);
            UpdateCameraBounds();
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            base.OnDestroy();
        }

        public FacilityData Find(string facilityId) =>
            facilityId != null && _facilities.TryGetValue(facilityId, out var data) ? data : null;

        public IEnumerable<FacilityData> Catalog => _facilities.Values;

        public FacilityCheck CanPlace(FacilityData data, Vector2Int origin, int rotation) =>
            FacilityRules.CanPlace(data, origin, rotation, _grid, _rooms, GuildRank, Gold, _settings);

        public FacilityCheck CanUpgrade(PlacedFacility room) => FacilityRules.CanUpgrade(room, GuildRank, Gold);

        public FacilityCheck CanDemolish(PlacedFacility room) => FacilityRules.CanDemolish(room, _grid, _hallId, _settings);

        // Pays and starts construction if still allowed (re-checked here). Null = nothing changed.
        public PlacedFacility TryPlace(FacilityData data, Vector2Int origin, int rotation)
        {
            var check = CanPlace(data, origin, rotation);
            if (!check.Allowed || !Spend(check.Gold))
                return null;

            PlacedFacility room = new(_nextId++, data, origin, rotation, 0, 1, FacilityState.Building,
                                      Now + (long)data.BuildDaysFor(1) * GameCalendar.MinutesPerDay, check.Gold);
            Add(room);
            GameEventsRelay.OnFacilityPlaced.Invoke(room);

            if (data.BuildDaysFor(1) == 0)
                Finish(room, isNewRoom: true);
            return room;
        }

        public bool TryUpgrade(PlacedFacility room)
        {
            var check = CanUpgrade(room);
            if (!check.Allowed || !Spend(check.Gold))
                return false;

            var days = room.Data.BuildDaysFor(room.Level + 1);
            room.StartUpgrade(check.Gold, Now + (long)days * GameCalendar.MinutesPerDay);
            _views.Refresh(room);
            GameEventsRelay.OnFacilityUpgradeStarted.Invoke(room);

            if (days == 0) Finish(room, isNewRoom: false);
            return true;
        }

        public bool TryDemolish(PlacedFacility room)
        {
            var check = CanDemolish(room);
            if (!check.Allowed)
                return false;

            if (check.Gold > 0 && GuildManager.HasInstance)
                GuildManager.Instance.Wallet.Add(check.Gold, GoldReason.Construction);

            _rooms.Remove(room);
            _grid.Remove(room);
            _views.Remove(room);
            UpdateCameraBounds();
            GameEventsRelay.OnFacilityRemoved.Invoke(room);
            return true;
        }

        // Debug and testing: every running construction finishes now.
        public void FinishAllNow()
        {
            foreach (var room in new List<PlacedFacility>(_rooms).Where(room => room.IsUnderConstruction))
            {
                Finish(room, isNewRoom: room.Level == 0);
            }
        }

        public void WriteTo(GameSaveData data)
        {
            var saved = _rooms.Select(room => room.ToSaved()).ToList();

            data.SetFacilities(saved);
            data.NextFacilityId = _nextId;
            data.FacilitiesInitialized = HasGuildHall;
        }

        // NOTE: Due constructions are collected first, then finished — never modifies the list while iterating it.
        private void OnTimeTick(GameDateTime now)
        {
            List<PlacedFacility> due = null;
            foreach (var room in _rooms.Where(room => room.IsUnderConstruction && now.TotalMinutes >= room.FinishAtMinutes))
            {
                (due ??= new List<PlacedFacility>()).Add(room);
            }

            if (due == null)
                return;
            foreach (var room in due)
                Finish(room, isNewRoom: room.Level == 0);
        }

        private void Finish(PlacedFacility room, bool isNewRoom)
        {
            room.FinishConstruction();
            _views.Refresh(room);

            if (isNewRoom && _settings.BuiltReputation != 0 && GuildManager.HasInstance)
                GuildManager.Instance.Reputation.Change(_settings.BuiltReputation, ReputationSource.FacilityBuilt);

            GameEventsRelay.OnFacilityCompleted.Invoke(room);
        }

        // NOTE: Only marks the hall as present once it really exists — a missing facility logs an error and the next load
        // tries again (no more saves claiming a hall that was never placed).
        private void PlaceGuildHall()
        {
            var hall = Find(_settings.GuildHallId);
            if (!hall)
            {
                Debug.LogError($"[FacilityManager] Guild Hall '{_settings.GuildHallId}' isn't in the facility database " +
                               "(run Database › Load and assign GameConfig › Facilities › Database).", this);
                return;
            }

            PlacedFacility room = new(_nextId++, hall, _settings.GuildHallOrigin, _settings.GuildHallRotation, 1, 1,
                                      FacilityState.Complete, 0, 0);
            Add(room);
            GameEventsRelay.OnFacilityPlaced.Invoke(room);
            GameEventsRelay.OnFacilityCompleted.Invoke(room);
        }

        private void Add(PlacedFacility room)
        {
            _rooms.Add(room);
            _grid.Add(room);
            if (room.Data.Id == _settings.GuildHallId && _hallId < 0)
                _hallId = room.Id;

            _views.Refresh(room);
            UpdateCameraBounds();
        }

        private static bool Spend(long gold) =>
            gold <= 0 || (GuildManager.HasInstance && GuildManager.Instance.Wallet.TrySpend(gold, GoldReason.Construction));

        // NOTE: The camera may roam over the union of all rooms' footprints (plus padding).
        private void UpdateCameraBounds()
        {
            if (!cameraBounds || _rooms.Count == 0)
                return;

            Vector2Int min = new(int.MaxValue, int.MaxValue), max = new(int.MinValue, int.MinValue);
            foreach (var room in _rooms)
            {
                var size = FacilityFootprint.RotatedSize(room.Data.Size, room.Rotation);
                min = Vector2Int.Min(min, room.Origin);
                max = Vector2Int.Max(max, room.Origin + size);
            }

            Vector3 a = _views.CellToWorld(min), b = _views.CellToWorld(max);
            var padding = _settings.CameraBoundsPadding * 2f;
            cameraBounds.SetArea((a + b) * 0.5f, new Vector2(Mathf.Abs(b.x - a.x) + padding, Mathf.Abs(b.z - a.z) + padding));
        }

        // NOTE: A room whose facility no longer exists is dropped with a warning (rooms beyond it may lose their link).
        // The "initialized" flag isn't trusted for the hall — Start checks for the hall itself.
        private void Restore(GameSaveData data)
        {
            if (data == null)
                return;

            _nextId = Mathf.Max(1, data.NextFacilityId);

            foreach (var saved in data.Facilities)
            {
                var facility = Find(saved.FacilityId);
                if (!facility)
                {
                    Debug.LogWarning($"[FacilityManager] Saved room #{saved.Id} uses facility '{saved.FacilityId}', which isn't in the database any more; dropped.", this);
                    continue;
                }

                PlacedFacility room = new(saved.Id, facility, saved.Origin, saved.Rotation, saved.Level, saved.TargetLevel,
                                          saved.State, saved.FinishAtMinutes, saved.TotalSpent);
                _rooms.Add(room);
                _grid.Add(room);
                if (facility.Id == _settings.GuildHallId && _hallId < 0)
                    _hallId = room.Id;
                _nextId = Mathf.Max(_nextId, saved.Id + 1);
            }
        }
    }
}