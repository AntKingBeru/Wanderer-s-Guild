using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes" on this component so it dies with the Game scene.
    public class TimeManager : Singleton<TimeManager>, ISaveContributor
    {
        private TimeSettings _settings;
        private GameCalendar _calendar;
        private GameClock _clock;
        private readonly TickAccumulator _ticks = new();
        private GameSpeed _speed = GameSpeed.Paused;
        private GameSpeed _lastRunningSpeed = GameSpeed.Normal;
        private GameSpeed _speedBeforeMenu;

        public GameCalendar Calendar => _calendar;
        public GameDateTime Now => _clock.Now;
        public GameSpeed Speed => _speed;
        public bool IsPaused => _speed == GameSpeed.Paused;

        // False until the scene has faded in; speed changes are ignored before that.
        public bool IsRunning { get; private set; }

        // True while a menu holds time paused; speed changes (hotkeys, HUD) are ignored meanwhile.
        public bool IsMenuPaused { get; private set; }

        // Progress toward the next tick (0–1), for smooth visuals between ticks.
        public float TickFraction => _ticks.Fraction;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Time;
            _calendar = new GameCalendar(_settings);
            _clock = new GameClock(_calendar, ResolveStartMinutes());
            TimeScaleSync.Apply(0f); // Nothing moves until the scene has faded in.
        }

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);

            TimeEventPublisher.PublishInitial(Now);
            GameEventsRelay.OnGameSpeedChanged.Invoke(_speed);

            // NOTE: Starts only once the screen has faded in, so no time passes under black.
            if (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy)
                GameEventsRelay.OnSceneTransitionCompleted.AddListener(OnTransitionCompleted);
            else
                BeginRunning();
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnSceneTransitionCompleted.RemoveListener(OnTransitionCompleted);
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);

            TimeScaleSync.Reset();
            base.OnDestroy();
        }

        // NOTE: Real (unscaled) time drives the ticks; deltaTime is already scaled by the speed and would double it.
        private void Update()
        {
            if (!IsRunning)
                return;

            var ticks = _ticks.Consume(Time.unscaledDeltaTime, TicksPerSecond(_speed), _settings.MaxTicksPerFrame);
            for (var i = 0; i < ticks; i++)
                Tick();
        }

        // Player-driven speed change (hotkeys, HUD). Ignored before the fade-in and while a menu holds the pause.
        public void SetSpeed(GameSpeed speed)
        {
            if (!IsRunning || IsMenuPaused)
                return;
            ApplySpeed(speed);
        }

        // Pauses, or resumes at the speed it was paused from.
        public void TogglePause() => SetSpeed(IsPaused ? _lastRunningSpeed : GameSpeed.Paused);

        // Pauses for a menu and remembers the current speed (Paused included).
        public void EnterMenuPause()
        {
            if (!IsRunning || IsMenuPaused)
                return;

            _speedBeforeMenu = _speed;
            ApplySpeed(GameSpeed.Paused);
            IsMenuPaused = true;
        }

        // Restores exactly the speed from before the menu opened.
        public void ExitMenuPause()
        {
            if (!IsMenuPaused)
                return;

            IsMenuPaused = false;
            ApplySpeed(_speedBeforeMenu);
        }

        public void WriteTo(GameSaveData data) => data.TotalMinutes = Now.TotalMinutes;

        private void ApplySpeed(GameSpeed speed)
        {
            if (speed == _speed)
                return;

            _speed = speed;
            if (speed != GameSpeed.Paused)
                _lastRunningSpeed = speed;

            TimeScaleSync.Apply(_settings.Multiplier(speed));
            GameEventsRelay.OnGameSpeedChanged.Invoke(speed);
        }

        private void OnTransitionCompleted(SceneId scene)
        {
            if (scene != SceneId.Game)
                return;

            GameEventsRelay.OnSceneTransitionCompleted.RemoveListener(OnTransitionCompleted);
            BeginRunning();
        }

        private void BeginRunning()
        {
            IsRunning = true;
            ApplySpeed(GameSpeed.Normal);
        }

        private void Tick()
        {
            var previous = Now;
            var now = _clock.Advance(_settings.MinutesPerTick);
            TimeEventPublisher.PublishTick(previous, now);
        }

        // NOTE: Ticks per real second = ticks per in-game day ÷ real seconds per day × the speed multiplier.
        private double TicksPerSecond(GameSpeed speed)
        {
            var ticksPerDay = (double)GameCalendar.MinutesPerDay / _settings.MinutesPerTick;
            return ticksPerDay / _settings.RealSecondsPerDay * _settings.Multiplier(speed);
        }

        // NOTE: SaveGameService (execution order −50) has already loaded the slot: exact saved time from game.json,
        // or the header date at the start hour. Without it, the clock starts like a new game.
        private long ResolveStartMinutes() =>
            SaveGameService.HasInstance
                ? SaveGameService.Instance.StartTotalMinutes
                : _calendar.ToTotalMinutes(GameConfig.Instance.NewGuild.StartingDate, _settings.StartHour);
    }
}