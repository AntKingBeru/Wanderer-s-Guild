// Game-scene owner of the roster: starting party, dawn arrivals (join, or apply when an office exists), XP, capacity, save.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Awake runs after SaveGameService (−50) has loaded; Start after
    // FacilityManager's (−30), so the Guild Hall's beds exist before the starting party joins. With an active Guild Master's
    // Office, arrivals become applicants (RecruitmentManager); otherwise they join directly.
    public class AdventurerManager : Singleton<AdventurerManager>, ISaveContributor
    {
        private readonly HashSet<string> _takenNames = new();
        private AdventurerSettings _settings;
        private Dictionary<string, ClassData> _classes = new();
        private Dictionary<string, SpeciesData> _species = new();
        private Dictionary<string, EquipmentData> _equipment = new();
        private AdventurerRoster _roster;
        private AdventurerFactory _factory;
        private int _nextId = 1;
        private int _lastCapacity = -1;
        private bool _startingAdventurersDone;
        private bool _warnedNoCapacityEffects;

        public IReadOnlyList<Adventurer> Adventurers => _roster.Adventurers;
        public int Capacity => _roster.Capacity;
        public bool IsFull => _roster.IsFull;
        
        // Every class by id (advancement offers, trials).
        public IReadOnlyDictionary<string, ClassData> Classes => _classes;

        // Every item by id (class gear swaps).
        public IReadOnlyDictionary<string, EquipmentData> Equipment => _equipment;

        private static bool UsesApplicants => RecruitmentManager.HasInstance && RecruitmentManager.Instance.AcceptsApplicants;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Adventurers;

            if (_settings.ClassDatabase != null)
                _classes = _settings.ClassDatabase.BuildLookup();
            if (_settings.SpeciesDatabase != null)
                _species = _settings.SpeciesDatabase.BuildLookup();
            var equipment = GameConfig.Instance.Equipment.Database;
            if (equipment != null)
                _equipment = equipment.BuildLookup();
            if (_classes.Count == 0 || _species.Count == 0)
                Debug.LogWarning("[AdventurerManager] Class or species database is empty or unassigned (run Database › Load) — nobody can arrive.", this);

            var baseClasses = _settings.ClassDatabase ? _settings.ClassDatabase.BaseClasses(_classes) : new List<ClassData>();
            _roster = new AdventurerRoster(CurrentCapacity);
            var allClasses = _settings.ClassDatabase ? new List<ClassData>(_settings.ClassDatabase.Classes) : new List<ClassData>();
            _factory = new AdventurerFactory(_settings, new List<SpeciesData>(_species.Values), baseClasses, allClasses,
                id => AdvancementManager.HasInstance && AdvancementManager.Instance.IsUnlocked(id),
                _equipment);

            Restore(SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null);
        }

        private void OnEnable()
        {
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnFacilityCompleted.AddListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityChanged);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityChanged);
        }

        // NOTE: A new guild (or an older save without a roster) gets its starting party, once — always directly.
        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);

            _lastCapacity = Capacity;
            if (_startingAdventurersDone)
                return;

            _startingAdventurersDone = true;
            JoinStartingParty();
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);

            base.OnDestroy();
        }

        public Adventurer Find(int id) => _roster.Find(id);

        // Adds XP; raises OnAdventurerLeveledUp once per level gained. Returns levels gained.
        public int GrantExperience(int adventurerId, long amount)
        {
            var adventurer = _roster.Find(adventurerId);
            if (adventurer == null)
                return 0;

            var gained = adventurer.AddExperience(amount);
            for (var i = 0; i < gained; i++)
                GameEventsRelay.OnAdventurerLeveledUp.Invoke(adventurer);
            return gained;
        }

        // Adds an adventurer to the roster (starting party, direct arrival, accepted applicant) and raises
        // OnAdventurerJoined. False when the guild is full.
        public bool TryAddMember(Adventurer adventurer)
        {
            if (!_roster.TryAdd(adventurer))
                return false;

            GameEventsRelay.OnAdventurerJoined.Invoke(adventurer);
            return true;
        }
        
        // The starting party joins regardless of beds (they may lodge outside) — raises OnAdventurerJoined.
        private void TryAddStartingMember(Adventurer adventurer)
        {
            if (!_roster.AddRestored(adventurer))
                return;

            GameEventsRelay.OnAdventurerJoined.Invoke(adventurer);
        }

        // Re-reads capacity and raises OnAdventurerCapacityChanged if it changed (rooms, outside lodgers).
        public void RefreshCapacity()
        {
            var capacity = Capacity;
            if (capacity == _lastCapacity)
                return;

            _lastCapacity = capacity;
            GameEventsRelay.OnAdventurerCapacityChanged.Invoke(capacity);
        }

        // Rebuilds a saved adventurer (roster or applicant). False if their species or class no longer exists.
        // Unknown item ids are dropped with a warning (the slot stays empty).
        public bool TryRebuild(SavedAdventurer saved, out Adventurer adventurer)
        {
            adventurer = null;
            if (string.IsNullOrEmpty(saved.SpeciesId) || !_species.TryGetValue(saved.SpeciesId, out var species) ||
                string.IsNullOrEmpty(saved.ClassId) || !_classes.TryGetValue(saved.ClassId, out var classData))
            {
                Debug.LogWarning($"[AdventurerManager] '{saved.Name}' has an unknown species '{saved.SpeciesId}' or class " +
                                 $"'{saved.ClassId}' and couldn't be loaded.", this);
                return false;
            }

            var ids = saved.EquipmentIds;
            var gear = new EquipmentData[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrEmpty(ids[i]))
                    continue;
                if (!_equipment.TryGetValue(ids[i], out gear[i]))
                    Debug.LogWarning($"[AdventurerManager] '{saved.Name}' wore unknown item '{ids[i]}'; dropped.", this);
            }

            adventurer = new Adventurer(saved.Id, saved.Name, species, classData, saved.Experience, saved.Rank, saved.Wealth,
                                        saved.Status, saved.JoinedAtMinutes, saved.RetainedGrowth, _settings.Experience,
                                        saved.RankProgress, saved.ExamReturnsAtMinutes, gear, saved.WellFedBonus,
                                        saved.Injury, saved.InjuredUntilMinutes, saved.ReturningUntilMinutes,
                                        saved.TrialClassId, saved.TrialReturnsAtMinutes);
            _nextId = Mathf.Max(_nextId, saved.Id + 1);
            return true;
        }

        // An arrival right now, ignoring the dawn chance (debug and testing): an applicant if the office is active,
        // otherwise a direct join. False if there's no room or nobody can be created.
        public bool ForceArrival() => Arrive();

        public void WriteTo(GameSaveData data)
        {
            var saved = _roster.Adventurers.Select(adventurer => adventurer.ToSaved()).ToList();

            data.SetAdventurers(saved);
            data.NextAdventurerId = _nextId;
            data.AdventurersInitialized = _startingAdventurersDone;
        }

        // NOTE: An entry without species or class is skipped with a warning; the rest still join.
        private void JoinStartingParty()
        {
            var party = _settings.StartingParty;
            for (var i = 0; i < party.Count; i++)
            {
                if (!party[i].IsValid)
                {
                    Debug.LogWarning($"[AdventurerManager] Starting party entry {i} has no species or class; skipped.", this);
                    continue;
                }

                FillTakenNames();
                if (_factory.TryCreateStarting(_nextId, party[i], RequestManager.NowMinutes, _takenNames, out var adventurer))
                {
                    _nextId++;
                    TryAddStartingMember(adventurer);
                }
            }
        }

        // NOTE: Beds from every finished room's CapacityEffect; the settings' capacity is only a fallback while no facility
        // has one (warned once). Read on demand, so it's right whatever order the managers' Awakes ran in.
        private int CurrentCapacity()
        {
            if (FacilityManager.HasInstance)
            {
                var (total, anyProvider) = FacilityEffects.Capacity(FacilityManager.Instance.Rooms);
                var outside = LodgingManager.HasInstance ? LodgingManager.Instance.OutsideCount : 0;
                if (anyProvider)
                    return total + outside;

                if (!_warnedNoCapacityEffects)
                {
                    _warnedNoCapacityEffects = true;
                    Debug.LogWarning("[AdventurerManager] No facility has a CapacityEffect — using the fallback capacity " +
                                     $"({_settings.Capacity}). Assign one to the Guild Hall and bedrooms.", this);
                }
            }
            return _settings.Capacity;
        }

        // NOTE: Only finished levels count, so only completion and removal can change capacity.
        private void OnFacilityChanged(PlacedFacility _)
        {
            RefreshCapacity();
        }

        // NOTE: One roll per day at dawn. With an office, the waiting list's room matters (a full guild still gets
        // applicants); without one, a full guild means nobody arrives. Nobody arrives while the start tutorial holds them back.
        private void OnHourStarted(GameDateTime now)
        {
            if (now.Minute != 0 || now.Hour != GameConfig.Instance.Time.DawnHour || !HasRoomForArrival)
                return;
            if (TutorialManager.HasInstance && TutorialManager.Instance.BlocksArrivals)
                return;

            if (Random.value < _settings.ArrivalChance(CurrentReputation))
                Arrive();
        }

        private bool HasRoomForArrival => UsesApplicants ? RecruitmentManager.Instance.HasRoom : !_roster.IsFull;

        private bool Arrive()
        {
            if (!HasRoomForArrival || !TryCreateCandidate(out var adventurer))
                return false;

            return UsesApplicants ? RecruitmentManager.Instance.AddApplicant(adventurer) : TryAddMember(adventurer);
        }

        private bool TryCreateCandidate(out Adventurer adventurer)
        {
            var rank = GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GameConfig.Instance.NewGuild.StartingRank;

            FillTakenNames();
            if (!_factory.TryCreate(_nextId, rank, CurrentReputation, RequestManager.NowMinutes, _takenNames, out adventurer))
            {
                Debug.LogWarning($"[AdventurerManager] Nobody could arrive: no species, or no base class unlocked at rank {rank}.", this);
                return false;
            }

            _nextId++;
            return true;
        }

        // NOTE: Names are unique across the roster AND the waiting applicants.
        private void FillTakenNames()
        {
            _takenNames.Clear();
            _takenNames.UnionWith(_roster.Names);
            if (RecruitmentManager.HasInstance)
                _takenNames.UnionWith(RecruitmentManager.Instance.Names);
        }

        private static ReputationLevel CurrentReputation =>
            GuildManager.HasInstance ? GuildManager.Instance.Reputation.Level : ReputationLevel.Neutral;

        // NOTE: Restored adventurers ignore capacity (nobody is dropped if a data change lowered it). XP above the class
        // cap is clamped on construction (they become the cap level).
        private void Restore(GameSaveData data)
        {
            if (data == null)
                return;

            _startingAdventurersDone = data.AdventurersInitialized;
            _nextId = Mathf.Max(1, data.NextAdventurerId);

            foreach (var saved in data.Adventurers)
            {
                if (TryRebuild(saved, out var adventurer))
                    _roster.AddRestored(adventurer);
            }
        }
        
        // Removes an adventurer for good (death) and raises OnAdventurerLeft. False if they're not in the roster.
        public bool RemoveMember(int adventurerId)
        {
            var adventurer = _roster.Find(adventurerId);
            if (adventurer == null || !_roster.Remove(adventurerId))
                return false;

            GameEventsRelay.OnAdventurerLeft.Invoke(adventurer);
            return true;
        }

        public bool TryGetClass(string classId, out ClassData classData)
        {
            classData = null;
            return !string.IsNullOrEmpty(classId) && _classes.TryGetValue(classId, out classData);
        }
    }
}