// Listens to TimeManager events and pushed updated string to the HUD text elements.
// Intentionally subscribes to only 3 events to keep the update surface minimal.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeHUDController : MonoBehaviour
{
    [Header("Time Display")]
    [Tooltip("Shows current time as HH:MM. Assign the TimLabel.")]
    [SerializeField] private TextMeshProUGUI timeLabel;
    
    [Tooltip("Shows time-of-day category in parentheses, e.g. '(Dawn)'. Assign TimeOfDayLabel.")]
    [SerializeField] private TextMeshProUGUI timeOfDayLabel;
    
    [Header("Date Display")]
    [Tooltip("Shows 'Day X, Month Y, Year Z'. Assign the DateLabel TextMeshProUGUI.")]
    [SerializeField] private TextMeshProUGUI dateLabel;

    [Tooltip("Displays the season sprite. Assign the SeasonIcon Image component.")]
    [SerializeField] private Image seasonIconImage;

    [Tooltip("Four sprites indexed by the Season enum order: 0=Spring, 1=Summer, 2=Autumn, 3=Winter. " +
             "Array length must be exactly 4.")]
    [SerializeField] private Sprite[] seasonSprites;
    
    #region Lifecycle
    private void OnEnable()
    {
        // Guard: TimeManager initializes via DontDestroyOnLoad in Awake, so by the time any OnEnable runs, it should exist.
        // This null check handles edge cases like additive scene loads or incorrect script execution order.
        if (!TimeManager.Instance)
        {
            Debug.LogWarning("[TimeHUDController]: TimeManager not found during OnEnable. " +
                             "Verify TimeManager is in the first loaded scene.");
            return;
        }

        TimeManager.Instance.OnMinuteChanged += HandleMinuteChanged;
        TimeManager.Instance.OnDayChanged += HandleDayChanged;
        TimeManager.Instance.OnSeasonChanged += HandleSeasonChanged;
    }

    private void OnDisable()
    {
        // Populate all fields immediately so nothing shows empty on the first frame.
        if (!TimeManager.Instance)
            return;
        
        RefreshTime(TimeManager.Instance.Hour, TimeManager.Instance.Minute);
        RefreshDate();
        RefreshSeasonIcon(TimeManager.Instance.GetCurrentSeason());
    }
    #endregion
    
    #region Event Handlers
    // Fires every in-game minute, which also covers hour changes (hour is already updated by the time this fires, so GetCurrentTimeOfDay() returns the new value).
    private void HandleMinuteChanged(int hour, int minute)
        => RefreshTime(hour, minute);
    
    // Fires once per in-game day, AFTER month and year rollovers are already applied.
    // Subscribing only here - rather than to all three date events - avoids three redundant string builds on a year rollover.
    private void HandleDayChanged(int day)
        => RefreshDate();

    private void HandleSeasonChanged(Season season)
        => RefreshSeasonIcon(season);
    #endregion
    
    #region Display Refresh
    private void RefreshTime(int hour, int minute)
    {
        if (timeLabel)
            timeLabel.text = $"{hour:D2}:{minute:D2}";

        if (timeOfDayLabel)
        {
            // TimeOfDay is re-queried (not cached) so it always reflects the current hour, including at the exact moment an hour boundary crosses.
            var tod = TimeManager.Instance.GetCurrentTimeOfDay().ToString();
            timeOfDayLabel.text = $"{tod}";
        }
    }

    private void RefreshDate()
    {
        if (!dateLabel || !TimeManager.Instance)
            return;

        dateLabel.text = TimeManager.Instance.GetFormattedDate();
    }

    private void RefreshSeasonIcon(Season season)
    {
        if (!seasonIconImage)
            return;
        
        var index = (int)season;
        
        // Bounds check guards against an unfinished seasonSprites array in the inspector.
        if (seasonSprites == null || index >= seasonSprites.Length || !seasonSprites[index])
        {
            Debug.LogWarning($"[TimeHUDController] No sprite assigned for Season index {index} " +
                             $"({season}). Assign all 4 sprites in the inspector.");
            return;
        }
        
        seasonIconImage.sprite = seasonSprites[index];
    }
    #endregion
}