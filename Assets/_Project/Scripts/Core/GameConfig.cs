// Central, early-loading configuration singleton exposing global tuning values to every system.
using UnityEngine;

namespace WanderersGuild
{
    // -101 guarantees config is ready before any default-order script's Awake.
    [DefaultExecutionOrder(-101)]
    public class GameConfig : Singleton<GameConfig>
    {
        [Header("Simulation Speed Multipliers")]
        [SerializeField] private float normalSpeed = 1f;
        [SerializeField] private float fastSpeed = 2f;
        [SerializeField] private float veryFastSpeed = 4f;
        
        [Header("Time — Tick")]
        [Tooltip("Real seconds between ticks at Normal speed. Lower = finer/faster clock.")]
        [SerializeField] private float secondsPerTickAtNormal = 0.1f;
        [Tooltip("Game-minutes advanced per tick. Keep below 60 for accurate hourly events.")]
        [SerializeField] private int gameMinutesPerTick = 5;
        [Tooltip("Max ticks processed in one frame — spiral-of-death guard after a hitch.")]
        [SerializeField] private int maxTicksPerFrame = 50;

        [Header("Time — Calendar")]
        [SerializeField] private int minutesPerHour = 60;
        [SerializeField] private int hoursPerDay = 24;
        [SerializeField] private int daysPerMonth = 30;
        [SerializeField] private int monthsPerYear = 12;

        [Header("Party Size Limits")]
        [SerializeField] private int minPartySizeLowRank = 2;
        [SerializeField] private int maxPartySizeLowRank = 5;
        [SerializeField] private int minPartySizeHighRank = 3;
        [SerializeField] private int maxPartySizeHighRank = 7;
        
        public float NormalSpeed => normalSpeed;
        public float FastSpeed => fastSpeed;
        public float VeryFastSpeed => veryFastSpeed;

        public float SecondsPerTickAtNormal => secondsPerTickAtNormal;
        public int GameMinutesPerTick => gameMinutesPerTick;
        public int MaxTicksPerFrame => maxTicksPerFrame;

        public int MinutesPerHour => minutesPerHour;
        public int HoursPerDay => hoursPerDay;
        public int DaysPerMonth => daysPerMonth;
        public int MonthsPerYear => monthsPerYear;

        public int MinPartySizeLowRank => minPartySizeLowRank;
        public int MaxPartySizeLowRank => maxPartySizeLowRank;
        public int MinPartySizeHighRank => minPartySizeHighRank;
        public int MaxPartySizeHighRank => maxPartySizeHighRank;

        // Maps a GameSpeed setting to its multiplier.
        public float GetSpeedMultiplier(GameSpeed speed) => speed switch
        {
            GameSpeed.Paused => 0f,
            GameSpeed.Normal => normalSpeed,
            GameSpeed.Fast => fastSpeed,
            GameSpeed.VeryFast => veryFastSpeed,
            _ => normalSpeed
        };
    }
}