// Game-scene owner of adventurer satisfaction: each member's mood, its level and effects (sign-up demands, the Happy bonus),
// the hourly update, the dawn tally, and saving. The facade the UI and quests ask.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Saved moods load in Awake (keyed by id); join events before Start
    // are ignored, and Start drops strays and gives everyone else a fresh mood (old saves, the starting party).
    public class SatisfactionManager : Singleton<SatisfactionManager>, ISaveContributor
    {
        private readonly Dictionary<int, AdventurerMood> _moods = new();
        private readonly List<SavedMood> _saved = new();
        private readonly List<int> _strays = new();
        private SatisfactionSettings _settings;
        private MoodEventListener _listener;
        private SatisfactionDepartures _departures;
        private bool _isReady; // NOTE: False until Start has synced with the roster.

        private static long Now => RequestManager.NowMinutes;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Satisfaction;
            _listener = new MoodEventListener(this, _settings);
            _departures = new SatisfactionDepartures(_settings);

            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data == null)
                return;
            foreach (var saved in data.Moods)
                _moods[saved.AdventurerId] = AdventurerMood.FromSaved(saved);
        }

        private void OnEnable()
        {
            _listener.Subscribe();
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnAdventurerJoined.AddListener(OnAdventurerJoined);
            GameEventsRelay.OnAdventurerLeft.AddListener(OnAdventurerLeft);
        }

        private void OnDisable()
        {
            _listener.Unsubscribe();
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnAdventurerJoined.RemoveListener(OnAdventurerJoined);
            GameEventsRelay.OnAdventurerLeft.RemoveListener(OnAdventurerLeft);
        }

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);
            if (!AdventurerManager.HasInstance)
                return;

            SyncWithRoster();
            _isReady = true;
            Refresh(false);
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            base.OnDestroy();
        }

        #region Queries
        // Satisfaction now (0–100).
        public float Score(Adventurer adventurer) => adventurer == null ? _settings.Baseline : Compute(adventurer, MoodOf(adventurer.Id), null);

        public SatisfactionLevel LevelOf(Adventurer adventurer) => SatisfactionRules.LevelFor(Score(adventurer), _settings);

        // The appeal a quest needs for them to sign up (QuestSignUps).
        public float RequiredAppeal(Adventurer adventurer) => SatisfactionRules.RequiredAppeal(LevelOf(adventurer), _settings);

        // Fills entries with every reason behind the score now (memories, then situations) and returns the score.
        public float Explain(Adventurer adventurer, List<MoodEntry> entries)
        {
            entries.Clear();
            return adventurer == null ? _settings.Baseline : Compute(adventurer, MoodOf(adventurer.Id), entries);
        }
        
        // The score and level now; with a reasons list, also fills it (the details pane).
        public MoodSnapshot Snapshot(Adventurer adventurer, List<MoodEntry> reasons = null)
        {
            var score = reasons != null ? Explain(adventurer, reasons) : Score(adventurer);
            return new MoodSnapshot(score, SatisfactionRules.LevelFor(score, _settings));
        }
        #endregion

        #region Changes
        // A memory of this reason (its tuned amount × scale), replacing any older one of the same reason.
        // NOTE: Only for current members — events about someone already removed (died in the same outcome) are ignored.
        public void Remember(Adventurer adventurer, MoodReason reason, float scale = 1f)
        {
            if (!_isReady || adventurer == null || AdventurerManager.Instance.Find(adventurer.Id) == null)
                return;
            if (!_settings.TryGetMemory(reason, out var value))
                return;

            var now = Now;
            MoodOf(adventurer.Id).Remember(new MoodMemory(reason, value.Amount * scale, now, now + value.DurationMinutes));
            RefreshOne(adventurer, true);
        }

        // Debug: a custom memory (amount 0 clears it).
        public void DebugRemember(Adventurer adventurer, float amount, float days)
        {
            if (!_isReady || adventurer == null)
                return;

            var now = Now;
            var end = now + (long)(Mathf.Max(0f, days) * GameCalendar.MinutesPerDay);
            MoodOf(adventurer.Id).Remember(new MoodMemory(MoodReason.Debug, amount, now, end));
            RefreshOne(adventurer, true);
        }

        // Debug: runs the dawn tally (idle days, Miserable days, departures) right now.
        public void DebugDawn()
        {
            if (!_isReady)
                return;
            Dawn();
            Refresh(true);
        }
        #endregion

        public void WriteTo(GameSaveData data)
        {
            _saved.Clear();
            foreach (var mood in _moods.Values)
                _saved.Add(mood.ToSaved());
            data.SetMoods(_saved);
        }

        private void OnHourStarted(GameDateTime now)
        {
            if (!_isReady)
                return;
            if (now.Minute == 0 && now.Hour == GameConfig.Instance.Time.DawnHour)
                Dawn();
            Refresh(true);
        }

        // NOTE: Idle days first (they feed the score), then each level is tallied; quitters leave after the loop.
        private void Dawn()
        {
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                var mood = MoodOf(adventurer.Id);
                mood.IdleDays = adventurer.Status switch
                {
                    AdventurerStatus.Idle => mood.IdleDays + 1,
                    AdventurerStatus.Injured => mood.IdleDays,
                    _ => 0
                };
                _departures.Tally(adventurer, mood, SatisfactionRules.LevelFor(Compute(adventurer, mood, null), _settings));
            }
            _departures.Flush();
        }

        // NOTE: Hourly (and once at Start, silently): drops faded memories, re-scores everyone, sets the Happy bonus and
        // announces level changes.
        private void Refresh(bool announce)
        {
            var now = Now;
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                MoodOf(adventurer.Id).Forget(now);
                RefreshOne(adventurer, announce);
            }
        }

        private void RefreshOne(Adventurer adventurer, bool announce)
        {
            var mood = MoodOf(adventurer.Id);
            var level = SatisfactionRules.LevelFor(Compute(adventurer, mood, null), _settings);
            adventurer.SetMorale(SatisfactionRules.PowerBonus(level, _settings));

            if (level == mood.Level)
                return;
            mood.Level = level;
            if (announce)
                GameEventsRelay.OnMoodChanged.Invoke(adventurer, level);
        }

        // NOTE: Faded-out memories (< 0.05) still count but aren't listed.
        private float Compute(Adventurer adventurer, AdventurerMood mood, List<MoodEntry> entries)
        {
            var now = Now;
            var memories = 0f;
            foreach (var memory in mood.Memories)
            {
                var value = memory.ValueAt(now);
                memories += value;
                if (entries != null && Mathf.Abs(value) >= 0.05f)
                    entries.Add(new MoodEntry(memory.Reason, value));
            }

            var situations = MoodSituations.Collect(adventurer, mood, _settings, entries);
            return SatisfactionRules.Score(_settings.Baseline, memories, situations);
        }

        // NOTE: Creates a fresh mood (with the New Arrival memory) for anyone without one.
        private AdventurerMood MoodOf(int adventurerId)
        {
            if (_moods.TryGetValue(adventurerId, out var mood))
                return mood;

            mood = new AdventurerMood(adventurerId);
            if (_settings.TryGetMemory(MoodReason.NewArrival, out var arrival))
            {
                var now = Now;
                mood.Remember(new MoodMemory(MoodReason.NewArrival, arrival.Amount, now, now + arrival.DurationMinutes));
            }
            _moods[adventurerId] = mood;
            return mood;
        }

        private void SyncWithRoster()
        {
            _strays.Clear();
            foreach (var id in _moods.Keys.Where(id => AdventurerManager.Instance.Find(id) == null))
            {
                _strays.Add(id);
            }
            foreach (var id in _strays)
                _moods.Remove(id);

            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
                MoodOf(adventurer.Id);
        }

        private void OnAdventurerJoined(Adventurer adventurer)
        {
            if (!_isReady)
                return;
            MoodOf(adventurer.Id);
            RefreshOne(adventurer, false);
        }

        private void OnAdventurerLeft(Adventurer adventurer) => _moods.Remove(adventurer.Id);
    }
}