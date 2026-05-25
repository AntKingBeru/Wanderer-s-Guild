using System;
using UnityEngine;

namespace QuestSystem
{
    /// <summary>
    /// Tracks in-game time when one full day = <see cref="realSecondsPerDay"/> real seconds.
    /// Default: 20 real minutes (1200 seconds) = 1 in-game day.
    /// Exposes:
    ///   - CurrentDay    (int, starts at 1)
    ///   - CurrentHour   (0–23)
    ///   - CurrentMinute (0–59)
    ///   - TotalGameMinutesElapsed
    ///   - OnDayChanged  event
    /// Static helper:
    ///   GameClock.FormatMinutesAsGameTime(int realMinutes)
    ///     Converts a real-minutes duration (used in QuestRequestSO.timeLimitMinutes)
    ///     into an in-game hours/minutes string, e.g.:
    ///       30  → "6h 0m"
    ///       5   → "1h 0m"
    ///       120 → "1d 0h"   (if it spans a full day)
    /// Conversion formula:
    ///   1 real minute = (24 * 60) / (realSecondsPerDay / 60) in-game minutes
    ///   With 20-minute days: 1 real minute = 1440 / 20 = 72 in-game minutes
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        // ── Config ────────────────────────────────────────────────────────────────
        [Header("Time Scale")]
        [Tooltip("How many real seconds equal one full in-game day. Default = 1200 (20 min).")]
        [SerializeField] private float realSecondsPerDay = 1200f;
        
        [Tooltip("Hour of day at which the game world starts (0–23).")]
        [SerializeField] [Range(0, 23)] private int startHour = 8;
        
        public static GameClock Instance { get; private set; }
        
        // ── State ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// Current in-game day, starting at 1.
        /// </summary>
        public int CurrentDay { get; private set; } = 1;
 
        /// <summary>
        /// Current in-game hour (0–23).
        /// </summary>
        public int CurrentHour { get; private set; }
 
        /// <summary>
        /// Current in-game minute (0–59).
        /// </summary>
        public int CurrentMinute { get; private set; }
 
        /// <summary>
        /// Total in-game minutes elapsed since the game started.
        /// </summary>
        public int TotalGameMinutesElapsed { get; private set; }
 
        /// <summary>
        /// Fired every time the in-game day increments.
        /// </summary>
        public event Action<int> OnDayChanged;
 
        // Real seconds elapsed (fractional)
        private float _realSecondsElapsed;
        
        // ── Derived constants ─────────────────────────────────────────────────────
        /// <summary>
        /// How many in-game minutes fit in one real second.
        /// </summary>
        private float GameMinutesPerRealSecond => (24f * 60f) / realSecondsPerDay;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initiate to startHour on day 1
            CurrentHour = startHour;
            CurrentMinute = 0;
            TotalGameMinutesElapsed = startHour * 60;
            
            // Seed _realSecondsElapsed so we resume correctly at startHour
            _realSecondsElapsed = (startHour * 60f) / GameMinutesPerRealSecond;
        }

        private void Update()
        {
            _realSecondsElapsed += Time.deltaTime;
            
            var totalGameMinutes = _realSecondsElapsed * GameMinutesPerRealSecond;
            var totalWholeMinutes = Mathf.FloorToInt(totalGameMinutes);

            if (totalWholeMinutes == TotalGameMinutesElapsed)
                return;
            
            TotalGameMinutesElapsed = totalWholeMinutes;
            
            var newDay = (TotalGameMinutesElapsed / (24 * 60)) + 1;
            var minuteOfDay = TotalGameMinutesElapsed % (24 * 60);
            var newHour = minuteOfDay / 60;
            var newMinute = minuteOfDay % 60;
            
            var dayChanged = newDay != CurrentDay;
            CurrentDay = newDay;
            CurrentHour = newHour;
            CurrentMinute = newMinute;
            
            if (dayChanged)
                OnDayChanged?.Invoke(CurrentDay);
        }
        
        // ── Static helpers (usable without an instance) ───────────────────────────
        /// <summary>
        /// Converts a quest time-limit expressed in real minutes into a human-readable
        /// in-game time string, using the current GameClock's real-seconds-per-day
        /// setting (falls back to 1200s if no instance exists).
        /// Examples (20-min day):
        ///   1  real min  →  72 game min  →  "1h 12m"
        ///   5  real min  →  360 game min →  "6h 0m"
        ///   30 real min  →  2160 game min → "1d 12h"
        /// </summary>
        public static string FormatMinutesAsGameTime(int realMinutes)
        {
            var secondsPerDay = Instance ? Instance.realSecondsPerDay : 1200f;
            var gameMinPerRealSec = (24f * 60f) / secondsPerDay;
            
            var totalGameMinutes = Mathf.RoundToInt(realMinutes * 60f * gameMinPerRealSec);
            
            var days = totalGameMinutes / (24 * 60);
            var hours = (totalGameMinutes % (24 * 60)) / 60;
            var minutes = totalGameMinutes % 60;
            
            if (days > 0)
                return hours > 0 ? $"{days}d {hours}h" : $"{days}d";
            
            if (hours > 0)
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
            
            return $"{minutes}m";
        }
        
        /// <summary>
        /// Returns the formatted current time as "HH:MM" (24-hour).
        /// </summary>
        public string GetTimeString() => $"{CurrentHour:D2}:{CurrentMinute:D2}";
        
        /// <summary>
        /// Returns the formatted current time in 12-hour format, e.g. "8:05 AM".
        /// </summary>
        public string GetTimeString12H()
        {
            var period = CurrentHour >= 12 ? "PM" : "AM";
            var h = CurrentHour % 12;
            
            if (h == 0)
                h = 12;
            
            return $"{h}:{CurrentMinute:D2} {period}";
        }
    }
}