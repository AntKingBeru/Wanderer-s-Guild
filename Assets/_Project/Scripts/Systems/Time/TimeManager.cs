// Simulation heartbeat: drives the discrete-tick clock, controls game speed, and broadcasts time events.
using UnityEngine;

namespace WanderersGuild
{
    // After config(-101)/data(-100), before default-order gameplay systems that subscribe to time.
    [DefaultExecutionOrder(-50)]
    public class TimeManager : Singleton<TimeManager>
    {
        private float _accumulator;
        private GameSpeed _lastActiveSpeed = GameSpeed.Normal;

        // Cached config (read once at startup).
        private float _secondsPerTick;
        private int _minutesPerTick;
        private int _maxTicksPerFrame;

        public GameClock Clock { get; private set; }
        public GameSpeed CurrentSpeed { get; private set; } = GameSpeed.Normal;

        public bool IsPaused => CurrentSpeed == GameSpeed.Paused;
        public GameDate CurrentDate => Clock.CurrentDate;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            var cfg = GameConfig.Instance;
            Clock = new GameClock(cfg.MinutesPerHour, cfg.HoursPerDay, cfg.DaysPerMonth, cfg.MonthsPerYear);
            _secondsPerTick = Mathf.Max(0.0001f, cfg.SecondsPerTickAtNormal);
            _minutesPerTick = Mathf.Max(1, cfg.GameMinutesPerTick);
            _maxTicksPerFrame = Mathf.Max(1, cfg.MaxTicksPerFrame);
        }

        private void Start()
        {
            // Broadcast opening state so UI/systems initialize to the right values.
            var relay = GameEventsRelay.Instance;
            relay.onGameSpeedChanged.Invoke(CurrentSpeed);
            relay.onDayChanged.Invoke(Clock.CurrentDate);
        }

        private void Update()
        {
            if (CurrentSpeed == GameSpeed.Paused)
                return;

            var multiplier = GameConfig.Instance.GetSpeedMultiplier(CurrentSpeed);
            // unscaledDeltaTime → sim speed is fully ours, independent of Unity's Time.timeScale.
            _accumulator += Time.unscaledDeltaTime * multiplier;

            var ticksThisFrame = 0;
            while (_accumulator >= _secondsPerTick && ticksThisFrame < _maxTicksPerFrame)
            {
                _accumulator -= _secondsPerTick;
                ticksThisFrame++;
                ProcessTick();
            }

            // Hit the cap (e.g. after a breakpoint/hitch)? Drop the backlog so we don't fast-forward wildly.
            if (ticksThisFrame >= _maxTicksPerFrame)
                _accumulator = 0f;
        }

        // One discrete step — advances the clock, then fires only the rollover events that occurred.
        private void ProcessTick()
        {
            var before = Clock.CurrentDate;
            var beforeHour = Clock.Hour;

            Clock.AdvanceMinutes(_minutesPerTick);

            var relay = GameEventsRelay.Instance;
            relay.onTick.Invoke(_minutesPerTick);

            if (Clock.Hour != beforeHour)
                relay.onHourChanged.Invoke(Clock.Hour);

            var after = Clock.CurrentDate;
            if (after.day != before.day || after.month != before.month || after.year != before.year)
                relay.onDayChanged.Invoke(after);
            if (after.season != before.season)
                relay.onSeasonChanged.Invoke(after.season);
        }

        // ---- Speed control API ----
        // Set an explicit speed (incl. Paused); remembers the last active speed for resume.
        public void SetSpeed(GameSpeed speed)
        {
            if (speed == CurrentSpeed)
                return;
            if (CurrentSpeed != GameSpeed.Paused)
                _lastActiveSpeed = CurrentSpeed;
            CurrentSpeed = speed;
            GameEventsRelay.Instance.onGameSpeedChanged.Invoke(CurrentSpeed);
        }

        public void Pause() => SetSpeed(GameSpeed.Paused);
        public void Resume() => SetSpeed(_lastActiveSpeed);
        public void TogglePause() => SetSpeed(IsPaused ? _lastActiveSpeed : GameSpeed.Paused);
    }
}