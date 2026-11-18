// Game-scene owner of the office log book: timestamped entries kept for the last N in-game days (and a cap), saved with the game.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". The saved log is read in Awake, so entries written during other
    // managers' Start (the starting party joining) come after it. Entries are oldest first (pruned from the front).
    public class GuildLog : Singleton<GuildLog>, ISaveContributor
    {
        private readonly List<LogEntry> _entries = new();
        private LogBookSettings _settings;

        // Oldest first.
        public IReadOnlyList<LogEntry> Entries => _entries;
        public int RetentionDays => _settings.RetentionDays;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.LogBook;

            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data != null)
                _entries.AddRange(data.LogEntries);
        }

        private void OnEnable() => GameEventsRelay.OnDayStarted.AddListener(OnDayStarted);
        private void OnDisable() => GameEventsRelay.OnDayStarted.RemoveListener(OnDayStarted);

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);
            Prune();
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            base.OnDestroy();
        }

        // Writes a line stamped with the current game time; old lines past the kept days drop off.
        public void Add(LogCategory category, LogTone tone, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            LogEntry entry = new(RequestManager.NowMinutes, category, tone, text);
            _entries.Add(entry);
            Prune();
            GameEventsRelay.OnLogEntryAdded.Invoke(entry);
        }

        public void WriteTo(GameSaveData data) => data.SetLogEntries(_entries);

        private void OnDayStarted(GameDateTime _) => Prune();

        // NOTE: Drops everything older than the kept days, then the oldest past the cap (both from the front).
        private void Prune()
        {
            var oldestKept = RequestManager.NowMinutes - (long)_settings.RetentionDays * GameCalendar.MinutesPerDay;
            var drop = 0;
            while (drop < _entries.Count && _entries[drop].Minutes < oldestKept)
                drop++;

            drop = Mathf.Max(drop, _entries.Count - _settings.MaxEntries);
            if (drop > 0)
                _entries.RemoveRange(0, drop);
        }
    }
}