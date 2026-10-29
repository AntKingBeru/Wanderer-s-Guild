// Game-scene owner of the request board: dawn and starting batches, expiry (−2 rep), dismissal (−1 rep), save/load.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Awake runs after SaveGameService (−50) has loaded.
    public class RequestManager : Singleton<RequestManager>, ISaveContributor
    {
        private RequestSettings _settings;
        private Dictionary<string, RequestData> _templates = new();
        private RequestBoard _board;
        private RequestGenerator _generator;
        private int _nextInstanceId = 1;
        private bool _startingBatchDone;

        public IReadOnlyList<ActiveRequest> Requests => _board.Requests;

        // Current in-game time (minutes); the save's start time if no TimeManager exists.
        public static long NowMinutes =>
            TimeManager.HasInstance ? TimeManager.Instance.Now.TotalMinutes
            : SaveGameService.HasInstance ? SaveGameService.Instance.StartTotalMinutes : 0L;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Requests;

            if (_settings.Database)
                _templates = _settings.Database.BuildLookup();
            if (_templates.Count == 0)
                Debug.LogWarning("[RequestManager] The request database is empty or unassigned (run Database › Load) — no requests will appear.", this);

            _board = new RequestBoard(_settings.BoardCapacity);
            _generator = new RequestGenerator(_settings, _templates.Values);
            Restore(SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null);
        }

        private void OnEnable()
        {
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnTimeTick.AddListener(OnTimeTick);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnTimeTick.RemoveListener(OnTimeTick);
        }

        // NOTE: A new guild (or an older save without request state) gets one starting batch, once.
        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);

            if (_startingBatchDone)
                return;

            _startingBatchDone = true;
            GenerateBatch(NowMinutes);
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);

            base.OnDestroy();
        }

        // Removes a request the player declined (−1 reputation by default).
        public bool Dismiss(int instanceId)
        {
            if (!_board.TryRemove(instanceId, out var request))
                return false;

            ChangeReputation(_settings.DismissReputation, ReputationSource.RequestDismissed);
            GameEventsRelay.OnRequestRemoved.Invoke(request, RequestEndReason.Dismissed);
            return true;
        }

        // Takes a request off the board to turn it into a guild quest (no penalty). Used by the quest system.
        public bool TryTake(int instanceId, out ActiveRequest request)
        {
            if (!_board.TryRemove(instanceId, out request))
                return false;

            GameEventsRelay.OnRequestRemoved.Invoke(request, RequestEndReason.Accepted);
            return true;
        }

        // Marks a request as viewed (clears its NEW tag). Saved with the request.
        public void MarkSeen(int instanceId)
        {
            foreach (var request in _board.Requests)
            {
                if (request.InstanceId != instanceId)
                    continue;

                request.MarkSeen();
                return;
            }
        }

        // Runs a batch immediately with the current rank and reputation (debug and testing).
        public void GenerateBatchNow() => GenerateBatch(NowMinutes);

        public void WriteTo(GameSaveData data)
        {
            var saved = _board.Requests.Select(request => request.ToSaved()).ToList();

            data.SetRequests(saved);
            data.NextRequestId = _nextInstanceId;
            data.RequestsInitialized = _startingBatchDone;
        }

        // NOTE: Expire first, then generate — a request ending at 05:00 frees its slot (and its template) for that dawn.
        private void OnHourStarted(GameDateTime now)
        {
            ExpireDue(now.TotalMinutes);

            if (now.Minute == 0 && now.Hour == GameConfig.Instance.Time.DawnHour)
                GenerateBatch(now.TotalMinutes);
        }

        private void OnTimeTick(GameDateTime now) => ExpireDue(now.TotalMinutes);

        private void GenerateBatch(long nowMinutes)
        {
            var rank = GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GameConfig.Instance.NewGuild.StartingRank;
            var reputation = GuildManager.HasInstance ? GuildManager.Instance.Reputation.Level : ReputationLevel.Neutral;

            var count = Mathf.Min(_settings.BatchSize(reputation), _board.FreeSlots);
            if (count <= 0)
                return;

            foreach (var request in from template in _generator
                         .Pick(count, rank, reputation, _board) let expiresAt = nowMinutes + (long)template.ExpirationDays * GameCalendar.MinutesPerDay
                     select new ActiveRequest(_nextInstanceId++, template, nowMinutes, expiresAt) into request where _board.TryAdd(request) select request)
            {
                GameEventsRelay.OnRequestAdded.Invoke(request);
            }
        }

        private void ExpireDue(long nowMinutes)
        {
            foreach (var request in _board.TakeExpired(nowMinutes))
            {
                ChangeReputation(_settings.ExpiryReputation, ReputationSource.RequestExpired);
                GameEventsRelay.OnRequestRemoved.Invoke(request, RequestEndReason.Expired);
            }
        }

        private static void ChangeReputation(int delta, ReputationSource source)
        {
            if (delta != 0 && GuildManager.HasInstance)
                GuildManager.Instance.Reputation.Change(delta, source);
        }

        // NOTE: Saved requests whose template no longer exists are dropped (with a warning). The next id stays above every
        // restored id so new postings never collide.
        private void Restore(GameSaveData data)
        {
            if (data == null)
                return;

            _startingBatchDone = data.RequestsInitialized;
            _nextInstanceId = Mathf.Max(1, data.NextRequestId);

            foreach (var saved in data.Requests)
            {
                if (string.IsNullOrEmpty(saved.TemplateId) || !_templates.TryGetValue(saved.TemplateId, out var template))
                {
                    Debug.LogWarning($"[RequestManager] Saved request '{saved.TemplateId}' isn't in the database any more; dropped.", this);
                    continue;
                }

                _board.TryAdd(new ActiveRequest(saved.InstanceId, template, saved.IssuedAtMinutes, saved.ExpiresAtMinutes, saved.Seen));
                _nextInstanceId = Mathf.Max(_nextInstanceId, saved.InstanceId + 1);
            }
        }
    }
}