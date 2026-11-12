// Shows adventurers' speech bubbles over the world: starts due lines from the speech queue, keeps each bubble above its
// speaker's head, and fades it out after a reading time.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: After the avatars' billboards (120) and the cards (130/131), so bubbles follow this frame's pose, hover and card.
    [DefaultExecutionOrder(135)]
    public class SpeechBubbleDirector : Singleton<SpeechBubbleDirector>
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldViewPresenter view;

        // One bubble on screen.
        private sealed class ActiveBubble
        {
            public SpeechBubbleView View;
            public AvatarInteractable Speaker;
            public float Elapsed;
            public float Duration;
            public bool IsChatter;
        }

        private readonly SpeechQueue _queue = new();
        private readonly SpeechLinePicker _picker = new();
        private readonly Dictionary<int, ActiveBubble> _active = new();
        private readonly Dictionary<int, float> _quietUntil = new();
        private readonly List<int> _ended = new();
        private SpeechTriggers _triggers;
        private SpeechBubblePool _pool;
        private SpeechSettings _settings;
        private Func<int, bool> _isPresent;
        private float _clock;          // NOTE: Real seconds the game has been running (not paused) — drives bubble timing.
        private int _chatterShown;

        private static long Now => RequestManager.NowMinutes;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Speech;
            _triggers = new SpeechTriggers(Say);
            _isPresent = IsPresent; // Cached once — no delegate allocation per frame.
        }

        private void OnEnable()
        {
            _triggers?.Bind();
            GameEventsRelay.OnAdventurerLeft.AddListener(OnAdventurerLeft);
            GameEventsRelay.OnBuildModeChanged.AddListener(OnBuildModeChanged);
        }

        private void OnDisable()
        {
            _triggers?.Unbind();
            GameEventsRelay.OnAdventurerLeft.RemoveListener(OnAdventurerLeft);
            GameEventsRelay.OnBuildModeChanged.RemoveListener(OnBuildModeChanged);
            EndAll();
        }

        // NOTE: In Start — the world view's document root is set in its OnEnable.
        private void Start()
        {
            _pool = view != null ? SpeechBubblePool.Bind(view.DocumentRoot, _settings.MaxVisible) : null;
            if (_pool == null)
                Debug.LogError("[SpeechBubbleDirector] Missing 'world-bubbles' in WorldView.uxml (or no view assigned).", this);
            if (_settings.Library == null)
                Debug.LogWarning("[SpeechBubbleDirector] GameConfig › Speech › Library is empty — adventurers stay quiet.", this);
        }

        // Queues a line for an adventurer (the triggers and the debug menu). delayMinutes: in-game minutes from now.
        public void Say(int adventurerId, SpeechTopic topic, int delayMinutes = 0)
        {
            var showAt = Now + Mathf.Max(0, delayMinutes);
            var waits = SpeechRules.WaitsForArrival(topic);
            var expires = showAt + (waits ? _settings.ArrivalWaitMinutes : _settings.LineWaitMinutes);
            _queue.Enqueue(new SpeechRequest(adventurerId, topic, SpeechRules.PriorityOf(topic), showAt, expires, waits),
                           _settings.MaxQueued);
        }

        // NOTE: Reading time runs in real seconds but only while the game runs: frozen when paused, the same at Fast speed.
        // No bubble starts while paused (it would sit at zero opacity behind the panel anyway).
        private void Update()
        {
            if (_pool == null)
                return;

            var running = Time.timeScale > 0f;
            var delta = running ? Time.unscaledDeltaTime : 0f;
            _clock += delta;

            var now = Now;
            _queue.Prune(now, _isPresent);
            Advance(delta);
            if (running)
                StartDue(now);
            Place();
        }

        // ---------- Bubbles on screen ----------

        // NOTE: A destroyed speaker (despawned at the door, died) ends its bubble — Unity null check.
        private void Advance(float delta)
        {
            _ended.Clear();
            foreach (var (id, bubble) in _active)
            {
                bubble.Elapsed += delta;
                if (bubble.Speaker == null || bubble.Elapsed >= bubble.Duration)
                    _ended.Add(id);
                else
                    bubble.View.SetOpacity(OpacityOf(bubble));
            }

            foreach (var id in _ended)
                End(id);
        }

        // NOTE: Hidden (timer still running) while its speaker is hovered (the name label is there) or has their card open,
        // or when the head is off-screen / behind the camera.
        private void Place()
        {
            var cardId = AdventurerCardController.HasInstance ? AdventurerCardController.Instance.OpenAdventurerId : -1;
            foreach (var (id, bubble) in _active)
            {
                var speaker = bubble.Speaker;
                if (speaker == null || speaker.IsHovered || id == cardId)
                {
                    bubble.View.SetVisible(false);
                    continue;
                }

                if (WorldViewProjection.TryToLocal(worldCamera, view, speaker.LabelAnchor, _settings.HeadGapPixels, out var local))
                    bubble.View.Place(local);
                else
                    bubble.View.SetVisible(false);
            }
        }

        private float OpacityOf(ActiveBubble bubble)
        {
            var fadeIn = _settings.FadeInSeconds > 0f ? Mathf.Clamp01(bubble.Elapsed / _settings.FadeInSeconds) : 1f;
            var fadeOut = _settings.FadeOutSeconds > 0f
                ? Mathf.Clamp01((bubble.Duration - bubble.Elapsed) / _settings.FadeOutSeconds)
                : 1f;
            return Mathf.Min(fadeIn, fadeOut);
        }

        // ---------- Starting lines ----------

        // NOTE: One bubble per adventurer, a short gap between their bubbles, and nothing new once the pool is empty
        // (event lines keep waiting in the queue until they expire).
        private void StartDue(long now)
        {
            if (!AvatarManager.HasInstance)
                return;

            foreach (var avatar in AvatarManager.Instance.Avatars.Values)
            {
                if (!_pool.HasFree)
                    return;
                if (avatar == null)
                    continue;

                var id = avatar.AdventurerId;
                if (_active.ContainsKey(id) || !_queue.HasAny(id))
                    continue;
                if (_quietUntil.TryGetValue(id, out var until) && _clock < until)
                    continue;

                if (_queue.TryTakeDue(id, now, out var request))
                    TryStart(avatar, request);
            }
        }

        // NOTE: A line taken but not shown is dropped: a leaving speaker only announces their departure; chatter past its cap
        // is just ambience.
        private void TryStart(AdventurerAvatar avatar, SpeechRequest request)
        {
            var chatter = SpeechRules.IsChatter(request.Topic);
            if (avatar.IsLeaving && !SpeechRules.AllowedWhileLeaving(request.Topic))
                return;
            if (chatter && _chatterShown >= _settings.MaxChatterVisible)
                return;
            if (!avatar.TryGetComponent(out AvatarInteractable speaker))
                return;
            if (!TryPickLine(request.Topic, avatar.PlannedFor, out var line))
                return;
            if (!_pool.TryRent(out var bubbleView))
                return;

            bubbleView.Show(line);
            _active[avatar.AdventurerId] = new ActiveBubble
            {
                View = bubbleView,
                Speaker = speaker,
                Duration = _settings.ReadingSeconds(line.Length),
                IsChatter = chatter
            };
            if (chatter)
                _chatterShown++;
        }

        // NOTE: Chatter becomes the current activity's kind (meal, shop, rest, sleep); a kind with no lines falls back to
        // plain chatter.
        private bool TryPickLine(SpeechTopic topic, AvatarActivity activity, out string line)
        {
            var library = _settings.Library;
            if (topic != SpeechTopic.Chatter)
                return _picker.TryPick(library, topic, out line);

            return _picker.TryPick(library, SpeechRules.ChatterFor(activity), out line) ||
                   _picker.TryPick(library, SpeechTopic.Chatter, out line);
        }

        private void End(int adventurerId)
        {
            if (!_active.Remove(adventurerId, out var bubble))
                return;

            _pool?.Return(bubble.View);
            if (bubble.IsChatter)
                _chatterShown--;
            _quietUntil[adventurerId] = _clock + _settings.GapSeconds;
        }

        private void EndAll()
        {
            _ended.Clear();
            _ended.AddRange(_active.Keys);
            foreach (var id in _ended)
                End(id);
        }

        // ---------- Events ----------

        private void OnAdventurerLeft(Adventurer adventurer)
        {
            _queue.Clear(adventurer.Id);
            End(adventurer.Id);
            _quietUntil.Remove(adventurer.Id);
        }

        private void OnBuildModeChanged(bool active) => _pool?.SetShown(!active);

        private static bool IsPresent(int adventurerId) =>
            AvatarManager.HasInstance && AvatarManager.Instance.TryGet(adventurerId, out var avatar) && avatar != null;
    }
}