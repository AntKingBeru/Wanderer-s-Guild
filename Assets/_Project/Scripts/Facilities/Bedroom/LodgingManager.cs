// Game-scene owner of lodging: houses every member (most free beds, or outside for the starting party), moves outside
// lodgers in when beds appear, re-houses residents of removed rooms, tracks the starter bedrooms, and saves it all.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Restores in Start; join events before then are ignored and
    // caught by Start's re-housing pass (covers any manager order and older saves).
    public class LodgingManager : Singleton<LodgingManager>, ISaveContributor
    {
        private readonly LodgingAssignments _assignments = new();
        private readonly HashSet<int> _starterRooms = new();
        private readonly List<int> _scratch = new();
        private LodgingSettings _settings;
        private IReadOnlyList<SavedLodging> _pendingLodgings;
        private IReadOnlyList<int> _pendingStarters;
        private int _lastStarterBuilt = -1;
        private bool _isReady;
        private bool _migrateStarters;

        public LodgingAssignments Assignments => _assignments;
        public int OutsideCount => _assignments.OutsideCount;

        private static IReadOnlyList<PlacedFacility> Rooms =>
            FacilityManager.HasInstance ? FacilityManager.Instance.Rooms : System.Array.Empty<PlacedFacility>();

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Lodging;

            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data == null)
                return;

            // NOTE: Saves from before lodging (v10) have no starter marks — their oldest bedrooms become the starters.
            _migrateStarters = data.Version < 10;
            _pendingLodgings = data.Lodgings;
            _pendingStarters = data.StarterRoomIds;
        }

        private void OnEnable()
        {
            GameEventsRelay.OnAdventurerJoined.AddListener(OnJoined);
            GameEventsRelay.OnAdventurerLeft.AddListener(OnLeft);
            GameEventsRelay.OnFacilityPlaced.AddListener(OnPlaced);
            GameEventsRelay.OnFacilityCompleted.AddListener(OnCompleted);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnRemoved);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnAdventurerJoined.RemoveListener(OnJoined);
            GameEventsRelay.OnAdventurerLeft.RemoveListener(OnLeft);
            GameEventsRelay.OnFacilityPlaced.RemoveListener(OnPlaced);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(OnCompleted);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnRemoved);
        }

        private void Start()
        {
            if (SaveGameService.HasInstance) SaveGameService.Instance.Register(this);

            Restore();
            _isReady = true;

            if (AdventurerManager.HasInstance)
            {
                foreach (var adventurer in AdventurerManager.Instance.Adventurers)
                {
                    if (!_assignments.Has(adventurer.Id))
                        House(adventurer, allowOutside: true);
                }
            }
            MoveOutsidersIn();
            PublishStarterProgress();
            RefreshCapacity();
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            base.OnDestroy();
        }

        // ---------- Queries ----------

        // Their room id (0 = outside), or −1 if not housed.
        public int RoomOf(int adventurerId) => _assignments.RoomOf(adventurerId);

        public bool LodgesOutside(int adventurerId) => _assignments.RoomOf(adventurerId) == LodgingAssignments.Outside;

        public bool IsStarter(PlacedFacility room) => room != null && _starterRooms.Contains(room.Id);

        // True if the next room of this facility would be a starter bedroom (exempt from its LIMIT).
        public bool StarterSlotOpen(FacilityData data) =>
            data != null && data == _settings.StarterFacility && _starterRooms.Count < _settings.StarterCount;

        // Finished starter bedrooms (the opening goal's progress).
        public int StarterBuilt
        {
            get
            {
                return Rooms.Count(room => _starterRooms.Contains(room.Id) && room.Level > 0);
            }
        }

        public int StarterTotal => _settings.StarterCount;

        // "Bedroom 2", "Lodging outside", or "No room".
        public string LabelFor(int adventurerId)
        {
            var roomId = RoomOf(adventurerId);
            if (roomId == LodgingAssignments.Outside)
                return "Lodging outside";

            var room = FindRoom(roomId);
            return room != null ? LodgingRules.Label(room, Rooms) : "No room";
        }

        // ---------- Player actions (21b UI) ----------

        // Moves an adventurer to a room with a free bed. False (nothing changes) if it has none or is their room.
        public bool TryMove(int adventurerId, int roomId)
        {
            var adventurer = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(adventurerId) : null;
            var room = FindRoom(roomId);
            if (adventurer == null || room == null || RoomOf(adventurerId) == roomId)
                return false;
            if (LodgingRules.FreeBeds(room, _assignments) <= 0)
                return false;

            var fromOutside = LodgesOutside(adventurerId);
            Assign(adventurer, roomId, fromOutside);
            return true;
        }

        public void WriteTo(GameSaveData data) => data.SetLodging(_assignments.ToSaved(), _starterRooms);

        // ---------- Events ----------

        private void OnJoined(Adventurer adventurer)
        {
            if (_isReady)
                House(adventurer, allowOutside: true);
        }

        private void OnLeft(Adventurer adventurer)
        {
            if (_assignments.Remove(adventurer.Id))
                RefreshCapacity();
        }

        // NOTE: Marked on placement (not completion), so the limit stays consistent while it's being built.
        private void OnPlaced(PlacedFacility room)
        {
            if (!StarterSlotOpen(room.Data))
                return;
            _starterRooms.Add(room.Id);
        }

        // NOTE: A finished level (new room or upgrade) can add beds — outside lodgers move in first.
        private void OnCompleted(PlacedFacility _)
        {
            if (!_isReady)
                return;
            MoveOutsidersIn();
            PublishStarterProgress();
            RefreshCapacity();
        }

        // NOTE: The demolish rule keeps enough beds for everyone; residents are re-housed (outside only as a warned last resort).
        private void OnRemoved(PlacedFacility room)
        {
            _starterRooms.Remove(room.Id);
            if (!_isReady)
                return;

            _assignments.ResidentsOf(room.Id, _scratch);
            foreach (var id in new List<int>(_scratch))
            {
                _assignments.Remove(id);
                var adventurer = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(id) : null;
                if (adventurer != null)
                    House(adventurer, allowOutside: false);
            }

            PublishStarterProgress();
            RefreshCapacity();
        }

        // ---------- Housing ----------

        private void House(Adventurer adventurer, bool allowOutside)
        {
            if (LodgingRules.TryBestRoom(Rooms, _assignments, out var room))
            {
                Assign(adventurer, room.Id, false);
                return;
            }

            if (!allowOutside)
                Debug.LogWarning($"[LodgingManager] No free bed for {adventurer.Name}; lodging them outside.", this);
            Assign(adventurer, LodgingAssignments.Outside, false);
        }

        private void MoveOutsidersIn()
        {
            _assignments.ResidentsOf(LodgingAssignments.Outside, _scratch);
            foreach (var id in new List<int>(_scratch))
            {
                if (!LodgingRules.TryBestRoom(Rooms, _assignments, out var room))
                    break;

                var adventurer = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(id) : null;
                if (adventurer != null)
                    Assign(adventurer, room.Id, true);
            }
        }

        private void Assign(Adventurer adventurer, int roomId, bool fromOutside)
        {
            _assignments.Assign(adventurer.Id, roomId);
            GameEventsRelay.OnLodgingChanged.Invoke(adventurer, roomId, fromOutside);
            RefreshCapacity();
        }

        private void PublishStarterProgress()
        {
            var built = StarterBuilt;
            if (built == _lastStarterBuilt)
                return;

            _lastStarterBuilt = built;
            GameEventsRelay.OnStarterLodgingsChanged.Invoke(built, _settings.StarterCount);
        }

        private static void RefreshCapacity()
        {
            if (AdventurerManager.HasInstance)
                AdventurerManager.Instance.RefreshCapacity();
        }

        private static PlacedFacility FindRoom(int roomId) => LodgingRules.FindRoom(Rooms, roomId);
        
        // The next bedroom (by id, wrapping) with a free bed, other than their own; null if none.
        public PlacedFacility NextFreeRoomFor(int adventurerId)
        {
            var current = RoomOf(adventurerId);
            PlacedFacility first = null, after = null;
            foreach (var room in Rooms)
            {
                if (room.Id == current || LodgingRules.FreeBeds(room, _assignments) <= 0)
                    continue;
                if (first == null || room.Id < first.Id)
                    first = room;
                if (room.Id > current && (after == null || room.Id < after.Id))
                    after = room;
            }
            return after ?? first;
        }

        // Moves them to <see cref="NextFreeRoomFor"/> (re-checked now). False if there's nowhere free.
        public bool TryMoveToNext(int adventurerId)
        {
            var room = NextFreeRoomFor(adventurerId);
            return room != null && TryMove(adventurerId, room.Id);
        }

        // NOTE: Drops entries for adventurers or rooms that are gone and rooms over their beds (re-housed in Start).
        private void Restore()
        {
            if (_pendingStarters != null)
            {
                foreach (var id in _pendingStarters)
                {
                    if (FindRoom(id) != null && _starterRooms.Count < _settings.StarterCount)
                        _starterRooms.Add(id);
                }
            }
            
            if (_migrateStarters && _settings.StarterFacility != null)
            {
                var bedrooms = Rooms.Where(room => room.Data == _settings.StarterFacility).ToList();
                bedrooms.Sort((a, b) => a.Id.CompareTo(b.Id));

                for (var i = 0; i < bedrooms.Count && _starterRooms.Count < _settings.StarterCount; i++)
                    _starterRooms.Add(bedrooms[i].Id);
            }

            if (_pendingLodgings != null && AdventurerManager.HasInstance)
            {
                foreach (var saved in _pendingLodgings)
                {
                    if (AdventurerManager.Instance.Find(saved.AdventurerId) == null)
                        continue;

                    if (saved.RoomId == LodgingAssignments.Outside)
                    {
                        _assignments.Assign(saved.AdventurerId, LodgingAssignments.Outside);
                        continue;
                    }

                    var room = FindRoom(saved.RoomId);
                    if (room != null && LodgingRules.FreeBeds(room, _assignments) > 0)
                        _assignments.Assign(saved.AdventurerId, saved.RoomId);
                }
            }

            _pendingStarters = null;
            _pendingLodgings = null;
        }
    }
}