using UnityEngine;
using TMPro;

namespace QuestSystem.UI
{
    /// <summary>
    /// Attach to a child of HUDCanvas.
    /// Updates a time text and an optional day counter every in-game minute.
    /// Hierarchy suggestion (add to HUDCanvas):
    /// Set use12HourFormat in the Inspector if you prefer "8:00 AM" style.
    /// </summary>
    public class WorldClockHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI dayText;
        
        [Header("Format")]
        [SerializeField] private bool use12HourFormat;
        
        private int _lastDisplayedMinute = -1;
        
        private void Update()
        {
            if (!GameClock.Instance)
                return;
 
            // Only rebuild the string when the minute actually changes
            if (GameClock.Instance.CurrentMinute == _lastDisplayedMinute)
                return;
            
            _lastDisplayedMinute = GameClock.Instance.CurrentMinute;
 
            if (timeText)
                timeText.text = use12HourFormat
                    ? GameClock.Instance.GetTimeString12H()
                    : GameClock.Instance.GetTimeString();
 
            if (dayText)
                dayText.text = $"Day {GameClock.Instance.CurrentDay}";
        }
    }
}