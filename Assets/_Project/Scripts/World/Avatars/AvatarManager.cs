// Game-scene owner of avatars: one per adventurer at the guild; arrivals one at a time and quest parties leaving together
// through the entrance; daily schedule at activity spots (sleep, supper, shopping, leisure); grid upkeep.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Builds the grid and syncs everyone in Start (after the managers
    // have loaded and the Guild Hall exists); join events before then are ignored — Start catches them.
    // NOTE: No `using System;` — it would make Random ambiguous (System.Action is written out instead).
    public class AvatarManager : Singleton<AvatarManager>, IProcessionHost
    {
        [SerializeField] private Grid grid;
        [SerializeField] private Camera worldCamera;
        [Tooltip("Spawned avatars go under this.")]
        [SerializeField] private Transform avatarsParent;

        private readonly Dictionary<int, AdventurerAvatar> _avatars = new();
        private readonly List<AdventurerAvatar> _snapshot = new();
        private readonly List<Vector2Int> _path = new();
        private readonly ArrivalQueue _arrivals = new();
        private GuildNavigator _navigator;
        private PartyProcessions _processions;
        private AvatarSettings _settings;
        private Vector2Int _entrance;
        private bool _hasEntrance;
        private bool _isReady;
        private bool _warnedEntrance;

        public IReadOnlyDictionary<int, AdventurerAvatar> Avatars => _avatars;
        public GuildNavigator Navigator => _navigator;

        private static long Now => RequestManager.NowMinutes;
        private static bool IsAtGuild(Adventurer a) =>
            a.Status is AdventurerStatus.Idle or AdventurerStatus.SignedUp or AdventurerStatus.Injured;

        // NOTE: At the guild and not lodging outside during sleep hours.
        private bool IsPresent(Adventurer a)
        {
            if (!IsAtGuild(a))
                return false;
            if (!LodgingManager.HasInstance || !LodgingManager.Instance.LodgesOutside(a.Id))
                return true;

            var minuteOfDay = (int)(Now % GameCalendar.MinutesPerDay);
            return !AvatarSchedule.IsSleepTime(minuteOfDay, _settings);
        }

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Avatars;
            _navigator = new GuildNavigator(grid);
            _processions = new PartyProcessions(this);
        }

        private void OnEnable()
        {
            GameEventsRelay.OnAdventurerJoined.AddListener(OnAdventurerJoined);
            GameEventsRelay.OnAdventurerLeft.AddListener(OnAdventurerLeft);
            GameEventsRelay.OnAdventurerChanged.AddListener(OnAdventurerChanged);
            GameEventsRelay.OnFacilityPlaced.AddListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityCompleted.AddListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilitiesChanged);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnLodgingChanged.AddListener(OnLodgingChanged);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnAdventurerJoined.RemoveListener(OnAdventurerJoined);
            GameEventsRelay.OnAdventurerLeft.RemoveListener(OnAdventurerLeft);
            GameEventsRelay.OnAdventurerChanged.RemoveListener(OnAdventurerChanged);
            GameEventsRelay.OnFacilityPlaced.RemoveListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilitiesChanged);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnLodgingChanged.RemoveListener(OnLodgingChanged);
        }

        // NOTE: On load everyone at the guild appears at a random free cell, then plans their first activity next frame.
        private void Start()
        {
            RebuildNav();
            _isReady = true;

            if (!AdventurerManager.HasInstance)
                return;
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (IsPresent(adventurer))
                    Spawn(adventurer, atEntrance: false);
            }
        }

        protected override void OnDestroy()
        {
            ActivitySpotRegistry.ClearReservations();
            PlaceholderAvatarSheets.Clear();
            AvatarFactory.ResetWarnings();
            base.OnDestroy();
        }

        public bool TryGet(int adventurerId, out AdventurerAvatar avatar) => _avatars.TryGetValue(adventurerId, out avatar);

        // Debug and testing: removes every avatar and spawns everyone at the guild again.
        public void RespawnAll()
        {
            _processions.Clear();
            _arrivals.Clear();
            foreach (var avatar in new List<AdventurerAvatar>(_avatars.Values))
                Despawn(avatar.AdventurerId);
            Start();
        }

        // ---------- Frame: furniture, processions, arrivals, the schedule ----------

        // NOTE: Furniture blocks follow the obstacle registry (the entrance is re-snapped if they changed). Processions and
        // arrivals run on scaled time (pause freezes them, like walking). Then one schedule read per frame: an avatar
        // re-plans when the schedule's activity differs from what it planned for, or when its leisure dwell runs out.
        // Leavers (incl. gathering parties) are skipped.
        private void Update()
        {
            if (!_isReady)
                return;

            if (_navigator.RefreshObstacles())
                ResolveEntrance();

            var delta = Time.deltaTime;
            _processions.Tick(delta);
            TickArrivals(delta);

            if (_avatars.Count == 0)
                return;

            var now = Now;
            var desired = DesiredNow(now);

            _snapshot.Clear();
            _snapshot.AddRange(_avatars.Values);
            foreach (var avatar in _snapshot)
            {
                if (avatar == null || avatar.IsLeaving)
                    continue;

                var adventurer = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(avatar.AdventurerId) : null;
                var wanted = adventurer is { Status: AdventurerStatus.Injured } ? AvatarActivity.Rest : desired;

                var scheduleChanged = avatar.PlannedFor != wanted;
                var dwellOver = !avatar.IsWalking && now >= avatar.NextDecisionAt;
                if (scheduleChanged || dwellOver)
                    Plan(avatar, wanted, now);
            }
        }

        // NOTE: Whoever's turn it is appears at the entrance — if still at the guild and not already shown.
        private void TickArrivals(float deltaTime)
        {
            var gap = GameConfig.Instance.Processions.ArrivalGapSeconds;
            while (_arrivals.TryNext(deltaTime, gap, out var id))
            {
                deltaTime = 0f;
                var adventurer = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(id) : null;
                if (adventurer != null && IsPresent(adventurer))
                    Spawn(adventurer, atEntrance: true);
            }
        }

        private AvatarActivity DesiredNow(long now)
        {
            var minuteOfDay = (int)(now % GameCalendar.MinutesPerDay);
            var hasTavern = TavernManager.HasInstance && TavernManager.Instance.Best.effect != null;
            var hasArmory = ArmoryManager.HasInstance && ArmoryManager.Instance.StockTier > 0;
            return AvatarSchedule.Desired(minuteOfDay, _settings, hasTavern, hasArmory,
                                          GameConfig.Instance.Tavern.SupperHour, GameConfig.Instance.Equipment.ShoppingHour);
        }

        // NOTE: Spot kind by activity; Sleep falls back to a Seat, everything falls back to wandering. Spot activities hold
        // until the schedule changes; leisure re-decides after its dwell.
        private void Plan(AdventurerAvatar avatar, AvatarActivity activity, long now)
        {
            ActivitySpotRegistry.Release(avatar.AdventurerId);
            avatar.PlannedFor = activity;

            var atSpot = activity switch
            {
                AvatarActivity.Sleep or AvatarActivity.Rest => TryGoToSpot(avatar, SpotKind.Bed, OwnRoom(avatar)) || TryGoToSpot(avatar, SpotKind.Seat),
                AvatarActivity.Supper => TryGoToSpot(avatar, SpotKind.Table),
                AvatarActivity.Shop => TryGoToSpot(avatar, SpotKind.Counter),
                _ => Random.value < _settings.SeatChance && TryGoToSpot(avatar, SpotKind.Seat)
            };

            var holds = atSpot && activity != AvatarActivity.Leisure;
            avatar.NextDecisionAt = holds ? long.MaxValue : now + Random.Range(_settings.DwellMinutes.x, _settings.DwellMinutes.y + 1);

            if (!atSpot)
                WanderTo(avatar);
        }

        // NOTE: −1 = any room (outside lodgers / unhoused never get someone else's bed: they get a seat).
        private static int OwnRoom(AdventurerAvatar avatar)
        {
            var room = LodgingManager.HasInstance ? LodgingManager.Instance.RoomOf(avatar.AdventurerId) : -1;
            return room > 0 ? room : int.MinValue;
        }

        // NOTE: Only spots whose cell is walkable (and, for a room id ≥ 0, inside that room — int.MinValue matches none) are
        // offered. Spots on furniture (chairs, beds) sit on blocked cells: the path ends at the nearest free cell beside the
        // spot (on the avatar's side), then the final step goes onto the spot; on arrival they face the spot's arrow and sit /
        // lie at its height (counters: standing).
        private bool TryGoToSpot(AdventurerAvatar avatar, SpotKind kind, int roomId = -1)
        {
            if (!ActivitySpotRegistry.TryReserve(kind, avatar.AdventurerId, spot =>
                {
                    var cell = CellOf(spot.Position);
                    return _navigator.IsWalkable(cell) && (roomId == -1 || _navigator.RoomOf(cell) == roomId);
                }, out var target))
                return false;

            var start = avatar.Cell;
            if (!_navigator.TryNearestFree(CellOf(target.Position), start, out var approach) ||
                !_navigator.TryFindPath(start, approach, _settings.MaxPathNodes, _path))
            {
                ActivitySpotRegistry.Release(avatar.AdventurerId);
                return false;
            }

            var facing = target.Facing;
            var surface = target.Position.y;
            var pose = PoseFor(kind);
            avatar.Walk(_path, target.Position, () =>
            {
                avatar.FaceTowards(facing);
                avatar.SetPose(pose, surface);
            });
            return true;
        }

        // NOTE: Free cells only. No path → just stand; the next dwell or schedule change tries again.
        private void WanderTo(AdventurerAvatar avatar)
        {
            if (!_navigator.TryRandomFreeCell(out var target))
                return;

            if (_navigator.TryFindPath(avatar.Cell, target, _settings.MaxPathNodes, _path))
                avatar.Walk(_path, null, null);
            else
                avatar.Stop();
        }

        // NOTE: Seats and tables seat them; beds lay them down; counters keep them standing.
        private static AvatarPose PoseFor(SpotKind kind) => kind switch
        {
            SpotKind.Seat or SpotKind.Table => AvatarPose.Sit,
            SpotKind.Bed => AvatarPose.Sleep,
            _ => AvatarPose.Stand
        };

        // ---------- Arrivals / departures ----------

        private void OnAdventurerJoined(Adventurer adventurer)
        {
            if (_isReady && IsPresent(adventurer))
                _arrivals.Enqueue(adventurer.Id);
        }

        private void OnAdventurerLeft(Adventurer adventurer)
        {
            _arrivals.Remove(adventurer.Id);
            Despawn(adventurer.Id);
        }

        // NOTE: Only at-guild ↔ away transitions matter (Signed up ↔ Idle keeps them here). Arrivals wait their turn at the
        // entrance; a quest party's members join its procession, everyone else walks out alone.
        private void OnAdventurerChanged(Adventurer adventurer)
        {
            if (!_isReady)
                return;

            var here = IsPresent(adventurer);
            var hasAvatar = _avatars.TryGetValue(adventurer.Id, out var avatar);

            switch (here)
            {
                case true when !hasAvatar:
                    _arrivals.Enqueue(adventurer.Id);
                    break;
                case true when avatar.IsLeaving:
                    avatar.IsLeaving = false; // Called back before reaching the door (a procession drops them next tick).
                    avatar.PlannedFor = AvatarActivity.None;
                    break;
                case false when hasAvatar && !avatar.IsLeaving:
                    if (!_processions.TryJoin(avatar, adventurer))
                        Leave(avatar);
                    break;
            }
        }

        // NOTE: At the entrance, or a random FREE cell (never on furniture).
        private void Spawn(Adventurer adventurer, bool atEntrance)
        {
            if (_avatars.ContainsKey(adventurer.Id))
                return;

            Vector2Int cell;
            if (atEntrance && _hasEntrance)
                cell = _entrance;
            else if (!_navigator.TryRandomFreeCell(out cell))
                return;

            var avatar = AvatarFactory.Create(adventurer, avatarsParent ? avatarsParent : transform,
                                                           worldCamera, grid, _settings);
            avatar.PlaceAt(cell);
            avatar.PlannedFor = AvatarActivity.None;
            avatar.NextDecisionAt = Now;
            _avatars[adventurer.Id] = avatar;
        }

        // NOTE: Frees their spot, walks to the entrance, then disappears; with no path, disappears right away.
        private void Leave(AdventurerAvatar avatar)
        {
            ActivitySpotRegistry.Release(avatar.AdventurerId);
            avatar.IsLeaving = true;
            var id = avatar.AdventurerId;

            if (_hasEntrance && _navigator.TryFindPath(avatar.Cell, _entrance, _settings.MaxPathNodes, _path))
                avatar.Walk(_path, null, () =>
                {
                    if (avatar.IsLeaving)
                        Despawn(id);
                });
            else
                Despawn(id);
        }

        private void Despawn(int adventurerId)
        {
            ActivitySpotRegistry.Release(adventurerId);
            if (!_avatars.Remove(adventurerId, out var avatar))
                return;

            if (avatar != null)
                Destroy(avatar.gameObject);
        }

        // ---------- IProcessionHost ----------

        public bool TryGetEntrance(out Vector2Int cell)
        {
            cell = _entrance;
            return _hasEntrance;
        }

        public bool TryWalkTo(AdventurerAvatar avatar, Vector2Int cell, System.Action onArrived)
        {
            if (avatar == null || !_navigator.TryFindPath(avatar.Cell, cell, _settings.MaxPathNodes, _path))
                return false;

            avatar.Walk(_path, null, onArrived);
            return true;
        }

        public void WalkOut(AdventurerAvatar avatar)
        {
            if (avatar != null)
                Leave(avatar);
        }

        public Vector3 CellCentre(Vector2Int cell)
        {
            var corner = grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
            return new Vector3(corner.x + 0.5f, _settings.FloorHeight, corner.z + 0.5f);
        }

        // ---------- Grid upkeep ----------

        // NOTE: Processions are dropped first (their cells and paths may be gone); then avatars on cells that are no longer
        // walkable go to the entrance; everyone re-plans, and leavers (incl. former procession members) re-path to the door.
        private void OnFacilitiesChanged(PlacedFacility _)
        {
            if (!_isReady)
                return;

            _processions.Clear();
            RebuildNav();
            foreach (var avatar in new List<AdventurerAvatar>(_avatars.Values))
            {
                if (!_navigator.IsWalkable(avatar.Cell))
                {
                    if (_hasEntrance)
                        avatar.PlaceAt(_entrance);
                    else
                    {
                        Despawn(avatar.AdventurerId);
                        continue;
                    }
                }

                if (avatar.IsLeaving)
                    Leave(avatar);
                else
                {
                    ActivitySpotRegistry.Release(avatar.AdventurerId);
                    avatar.Stop();
                    avatar.PlannedFor = AvatarActivity.None;
                }
            }
        }

        // NOTE: Outside lodgers walk out at bedtime and come back at dawn (OnAdventurerChanged does the spawn/leave).
        private void OnHourStarted(GameDateTime _)
        {
            if (!_isReady || !AdventurerManager.HasInstance)
                return;
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
                OnAdventurerChanged(adventurer);
        }

        // NOTE: Re-checks presence first (an outside lodger housed at night walks out now), then re-plans where they sleep.
        private void OnLodgingChanged(Adventurer adventurer, int _, bool __)
        {
            OnAdventurerChanged(adventurer);
            if (_avatars.TryGetValue(adventurer.Id, out var avatar) && !avatar.IsLeaving)
                avatar.PlannedFor = AvatarActivity.None;
        }

        private void RebuildNav()
        {
            if (FacilityManager.HasInstance)
                _navigator.Rebuild(FacilityManager.Instance.Rooms);
            ResolveEntrance();
        }

        // NOTE: The entrance marker's cell, snapped to the nearest FREE cell in its room; without a marker, any free cell
        // (warned once).
        private void ResolveEntrance()
        {
            _hasEntrance = false;
            if (GuildEntrance.Current != null && grid != null)
            {
                var marker = CellOf(GuildEntrance.Current.transform.position);
                _hasEntrance = _navigator.TryNearestFree(marker, marker, out _entrance);
                return;
            }

            if (!_warnedEntrance)
            {
                _warnedEntrance = true;
                Debug.LogWarning("[AvatarManager] No GuildEntrance in the Guild Hall prefab — adventurers enter at a random cell.", this);
            }
            _hasEntrance = _navigator.TryRandomFreeCell(out _entrance);
        }

        private Vector2Int CellOf(Vector3 position)
        {
            var cell = grid.WorldToCell(position);
            return new Vector2Int(cell.x, cell.y);
        }
    }
}