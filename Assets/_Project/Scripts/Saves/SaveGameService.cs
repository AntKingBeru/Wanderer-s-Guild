// Game-scene save system: loads the active slot when the scene starts; saves it (manual, daily autosave, exit).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: −50 so the loaded data exists before TimeManager / GuildManager read it in Awake. Scene-scoped: untick
    // Persist Across Scenes.
    [DefaultExecutionOrder(-50)]
    public class SaveGameService : Singleton<SaveGameService>
    {
        [Tooltip("The game camera — rendered (without UI) for the slot thumbnail.")]
        [SerializeField] private Camera thumbnailCamera;

        private readonly List<ISaveContributor> _contributors = new();
        private GameCalendar _calendar;
        private GameDataStore _dataStore;
        private bool _warnedNoSession;

        // Guild name (kept here until a guild system owns it) and the rank from the slot header.
        public string GuildName { get; private set; } = string.Empty;
        public GuildRank Rank { get; private set; }

        // Where TimeManager starts the clock and PlayTimeTracker its counter.
        public long StartTotalMinutes { get; private set; }
        public double StartPlayTimeSeconds { get; private set; }

        // The game.json read at scene start, or null (new guild, missing or unreadable file).
        public GameSaveData LoadedData { get; private set; }

        public bool IsSaving { get; private set; }

        // False without an active slot (e.g. Play started in the Game scene) or while a save runs.
        public bool CanSave => GameSession.HasActiveSlot && SaveSlotManager.HasInstance && !IsSaving;

        protected override void OnSingletonAwake()
        {
            _calendar = new GameCalendar(GameConfig.Instance.Time);
            LoadActiveSlot();
        }

        private void OnEnable() => GameEventsRelay.OnDayStarted.AddListener(OnDayStarted);
        private void OnDisable() => GameEventsRelay.OnDayStarted.RemoveListener(OnDayStarted);

        public void Register(ISaveContributor contributor)
        {
            if (contributor != null && !_contributors.Contains(contributor))
                _contributors.Add(contributor);
        }

        public void Unregister(ISaveContributor contributor) => _contributors.Remove(contributor);

        // Saves with toast feedback; waits a frame first so "Saving..." is on screen during the work.
        public async Awaitable<bool> SaveAsync(SaveReason reason)
        {
            if (!CheckCanSave())
                return false;

            IsSaving = true;
            GameEventsRelay.OnGameSaveStarted.Invoke(reason);
            var saved = false;

            try
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
                saved = WriteSave(captureThumbnail: true);
            }
            catch (OperationCanceledException)
            {
                // Scene unloading mid-save.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                IsSaving = false;
                GameEventsRelay.OnGameSaveCompleted.Invoke(reason, saved);
            }

            return saved;
        }

        // Saves immediately on this frame (used when quitting). No thumbnail: the previous one is kept.
        public bool SaveNow(SaveReason reason)
        {
            if (!CheckCanSave())
                return false;

            IsSaving = true;
            GameEventsRelay.OnGameSaveStarted.Invoke(reason);
            var saved = false;

            try
            {
                saved = WriteSave(captureThumbnail: false);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                IsSaving = false;
                GameEventsRelay.OnGameSaveCompleted.Invoke(reason, saved);
            }

            return saved;
        }

        // NOTE: Order matters — game data, then thumbnail, then the header last. The header is what the Load list
        // reads, so a failure part-way never leaves a header pointing at missing or older data.
        private bool WriteSave(bool captureThumbnail)
        {
            var slot = GameSession.ActiveSlot;
            var slots = SaveSlotManager.Instance.Store;

            // NOTE: Starts from a copy of what was loaded, so a system absent this session keeps its saved values.
            var data = LoadedData != null ? LoadedData.CloneAsCurrent() : new GameSaveData();
            if (LoadedData is not { HasGuildState: true })
                data.Rank = Rank; // Header rank until GuildManager writes the real one.

            foreach (var contributor in _contributors)
                contributor.WriteTo(data); // NOTE: All in one frame — one consistent snapshot.

            if (!_dataStore.TryWrite(slot, data))
                return false;

            if (captureThumbnail)
                TryWriteThumbnail(slots, slot);

            var date = _calendar.FromTotalMinutes(data.TotalMinutes).Date;
            var header = SaveSlotHeader.Create(GuildName, data.Rank, date, data.PlayTimeSeconds, DateTime.UtcNow);
            if (!slots.TryWriteHeader(slot, header))
                return false;

            LoadedData = data; // NOTE: The next save builds on this one.
            return true;
        }

        // NOTE: A thumbnail failure doesn't fail the save — the slot just keeps its previous picture.
        private void TryWriteThumbnail(SaveSlotStore slots, int slot)
        {
            if (!thumbnailCamera)
                return;

            Texture2D thumbnail = null;
            try
            {
                thumbnail = ThumbnailCapture.Capture(thumbnailCamera, GameConfig.Instance.SaveGame.ThumbnailRenderSize,
                                                     GameConfig.Instance.SaveSlots.ThumbnailSize);
                slots.TryWriteThumbnail(slot, thumbnail);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveGameService] Couldn't capture the thumbnail. {exception.Message}");
            }
            finally
            {
                if (thumbnail)
                    Destroy(thumbnail);
            }
        }

        // NOTE: Header first (name, rank, date, play time), then game.json overrides time and play time when present.
        private void LoadActiveSlot()
        {
            var config = GameConfig.Instance;
            GuildName = "Guild";
            Rank = config.NewGuild.StartingRank;
            StartTotalMinutes = _calendar.ToTotalMinutes(config.NewGuild.StartingDate, config.Time.StartHour);
            StartPlayTimeSeconds = 0d;
            LoadedData = null;

            if (!GameSession.HasActiveSlot || !SaveSlotManager.HasInstance)
            {
                Debug.LogWarning("[SaveGameService] No active save slot (Play started in the Game scene?) — starting fresh; saving is off.", this);
                _warnedNoSession = true;
                return;
            }

            var slot = GameSession.ActiveSlot;
            _dataStore = new GameDataStore(SaveSlotManager.Instance.Store, config.SaveSlots);

            var info = SaveSlotManager.Instance.GetSlot(slot);
            if (info.State == SaveSlotState.Ready)
            {
                GuildName = info.Header.GuildName;
                Rank = info.Header.Rank;
                StartTotalMinutes = _calendar.ToTotalMinutes(info.Header.Date, config.Time.StartHour);
                StartPlayTimeSeconds = info.Header.PlayTimeSeconds;
            }

            if (_dataStore.TryRead(slot, out var data))
            {
                LoadedData = data;
                StartTotalMinutes = data.TotalMinutes;
                StartPlayTimeSeconds = data.PlayTimeSeconds;
            }
        }

        private bool CheckCanSave()
        {
            if (CanSave && _dataStore != null)
                return true;

            if (!GameSession.HasActiveSlot && !_warnedNoSession)
            {
                Debug.LogWarning("[SaveGameService] No active save slot — nothing to save into.", this);
                _warnedNoSession = true;
            }
            return false;
        }

        private void OnDayStarted(GameDateTime _)
        {
            if (GameConfig.Instance.SaveGame.AutosaveAtDayStart)
                AutosaveAsync();
        }

        // NOTE: async void entry point for an event; SaveAsync already reports every outcome.
        private async void AutosaveAsync()
        {
            try
            {
                await SaveAsync(SaveReason.Autosave);
            }
            catch (OperationCanceledException)
            {
                // Scene unloading.
            }
        }

        // NOTE: Covers closing the window, Alt+F4 and stopping Play Mode. Synchronous and without a thumbnail —
        // rendering during shutdown isn't reliable.
        private void OnApplicationQuit()
        {
            if (GameConfig.Instance.SaveGame.SaveOnQuit && CanSave)
                SaveNow(SaveReason.Exit);
        }
    }
}