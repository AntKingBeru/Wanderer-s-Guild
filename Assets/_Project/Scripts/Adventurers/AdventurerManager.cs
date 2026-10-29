// Game-scene owner of the roster: starting adventurers, dawn arrivals (chance and level by reputation), XP, save/load.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Awake runs after SaveGameService (−50) has loaded.
    // Until the Guild Master's Office exists, arrivals join automatically (they become applicants once it's built).
    public class AdventurerManager : Singleton<AdventurerManager>, ISaveContributor
    {
        private AdventurerSettings _settings;
        private Dictionary<string, ClassData> _classes = new();
        private Dictionary<string, SpeciesData> _species = new();
        private AdventurerRoster _roster;
        private AdventurerFactory _factory;
        private int _nextId = 1;
        private bool _startingAdventurersDone;

        public IReadOnlyList<Adventurer> Adventurers => _roster.Adventurers;
        public int Capacity => _roster.Capacity;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Adventurers;

            if (_settings.ClassDatabase)
                _classes = _settings.ClassDatabase.BuildLookup();
            if (_settings.SpeciesDatabase)
                _species = _settings.SpeciesDatabase.BuildLookup();
            if (_classes.Count == 0 || _species.Count == 0)
                Debug.LogWarning("[AdventurerManager] Class or species database is empty or unassigned (run Database › Load) — nobody can arrive.", this);

            var baseClasses = _settings.ClassDatabase ? _settings.ClassDatabase.BaseClasses(_classes) : new List<ClassData>();
            _roster = new AdventurerRoster(_settings.Capacity);
            _factory = new AdventurerFactory(_settings, new List<SpeciesData>(_species.Values), baseClasses);

            Restore(SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null);
        }

        private void OnEnable() => GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
        private void OnDisable() => GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);

        // NOTE: A new guild (or an older save without a roster) gets its starting adventurers, once.
        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);

            if (_startingAdventurersDone)
                return;

            _startingAdventurersDone = true;
            for (var i = 0; i < _settings.StartingAdventurers; i++)
                TryRecruit();
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

        // An arrival right now, ignoring the dawn chance (debug and testing). False if full or nobody can be created.
        public bool ForceArrival() => TryRecruit();

        public void WriteTo(GameSaveData data)
        {
            var saved = _roster.Adventurers.Select(adventurer => adventurer.ToSaved()).ToList();

            data.SetAdventurers(saved);
            data.NextAdventurerId = _nextId;
            data.AdventurersInitialized = _startingAdventurersDone;
        }

        // NOTE: One roll per day at dawn; a full guild means nobody arrives.
        private void OnHourStarted(GameDateTime now)
        {
            if (now.Minute != 0 || now.Hour != GameConfig.Instance.Time.DawnHour || _roster.IsFull)
                return;

            if (Random.value < _settings.ArrivalChance(CurrentReputation))
                TryRecruit();
        }

        private bool TryRecruit()
        {
            if (_roster.IsFull)
                return false;

            var rank = GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GameConfig.Instance.NewGuild.StartingRank;
            if (!_factory.TryCreate(_nextId, rank, CurrentReputation, RequestManager.NowMinutes, _roster.Names, out Adventurer adventurer))
            {
                Debug.LogWarning($"[AdventurerManager] Nobody could arrive: no species, or no base class unlocked at rank {rank}.", this);
                return false;
            }

            _nextId++;
            if (!_roster.TryAdd(adventurer))
                return false;

            GameEventsRelay.OnAdventurerJoined.Invoke(adventurer);
            return true;
        }

        private static ReputationLevel CurrentReputation =>
            GuildManager.HasInstance ? GuildManager.Instance.Reputation.Level : ReputationLevel.Neutral;

        // NOTE: An adventurer whose species or class no longer exists can't be rebuilt — dropped with a warning naming them.
        // Rank progress and exam times default to 0 in older saves (no progress, no exam).
        private void Restore(GameSaveData data)
        {
            if (data == null)
                return;

            _startingAdventurersDone = data.AdventurersInitialized;
            _nextId = Mathf.Max(1, data.NextAdventurerId);

            foreach (var saved in data.Adventurers)
            {
                if (string.IsNullOrEmpty(saved.SpeciesId) || !_species.TryGetValue(saved.SpeciesId, out var species) ||
                    string.IsNullOrEmpty(saved.ClassId) || !_classes.TryGetValue(saved.ClassId, out var classData))
                {
                    Debug.LogWarning($"[AdventurerManager] '{saved.Name}' has an unknown species '{saved.SpeciesId}' or class " +
                                     $"'{saved.ClassId}' and couldn't be loaded.", this);
                    continue;
                }

                Adventurer adventurer = new(saved.Id, saved.Name, species, classData, saved.Experience, saved.Rank, saved.Wealth,
                                            saved.Status, saved.JoinedAtMinutes, saved.RetainedGrowth, _settings.Experience,
                                            saved.RankProgress, saved.ExamReturnsAtMinutes);
                _roster.TryAdd(adventurer);
                _nextId = Mathf.Max(_nextId, saved.Id + 1);
            }
        }
    }
}