// Listens to time events through GameEventRelay and updates the HUD text elements.
// Subscribes in OnEnable and unsubscribes in OnDisable; safe across scene loads.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeHUDController : MonoBehaviour
{
    [Header("Time Display")]
    [Tooltip("Shows current time as HH:MM.")]
    [SerializeField] private TextMeshProUGUI timeLabel;

    [Tooltip("Shows time-of-day category, e.g. '(Dawn)'.")]
    [SerializeField] private TextMeshProUGUI timeOfDayLabel;

    [Header("Date Display")]
    [Tooltip("Shows 'Day X, Month Y, Year Z'.")]
    [SerializeField] private TextMeshProUGUI dateLabel;

    [Tooltip("Displays the current season sprite.")]
    [SerializeField] private Image seasonIconImage;

    [Tooltip("Four sprites indexed by Season enum: 0=Spring, 1=Summer, 2=Autumn, 3=Winter.")]
    [SerializeField] private Sprite[] seasonSprites;
    
    #region Lifecycle
    private void OnEnable()
    {
        if (!GameEventRelay.Instance)
        {
            Debug.LogWarning("[TimeHUDController] GameEventRelay not found during OnEnable.");
            return;
        }
        // Subscribe through the relay; guaranteed to exist before any OnEnable fires.
        GameEventRelay.Instance.OnMinuteChanged.AddListener(HandleMinuteChanged);
        GameEventRelay.Instance.OnDayChanged.AddListener(HandleDayChanged);
        GameEventRelay.Instance.OnSeasonChanged.AddListener(HandleSeasonChanged);

        // Populate immediately so nothing shows empty on the first frame.
        if (TimeManager.Instance)
        {
            RefreshTime(TimeManager.Instance.Hour, TimeManager.Instance.Minute);
            RefreshDate();
            RefreshSeasonIcon(TimeManager.Instance.GetCurrentSeason());
        }
    }

    private void OnDisable()
    {
        if (!GameEventRelay.Instance) return;
        GameEventRelay.Instance.OnMinuteChanged.RemoveListener(HandleMinuteChanged);
        GameEventRelay.Instance.OnDayChanged.RemoveListener(HandleDayChanged);
        GameEventRelay.Instance.OnSeasonChanged.RemoveListener(HandleSeasonChanged);
    }
    #endregion
    
    #region Event Handlers
    // Fires every in-game minute; hour is already updated when this fires.
    private void HandleMinuteChanged(int hour, int minute)
        => RefreshTime(hour, minute);

    // Fires once per day after all rollovers; one subscription covers month and year changes too.
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

        if (timeOfDayLabel && TimeManager.Instance)
            timeOfDayLabel.text = TimeManager.Instance.GetCurrentTimeOfDay().ToString();
    }

    private void RefreshDate()
    {
        if (dateLabel && TimeManager.Instance)
            dateLabel.text = TimeManager.Instance.GetFormattedDate();
    }

    private void RefreshSeasonIcon(Season season)
    {
        if (!seasonIconImage)
            return;
        var index = (int)season;
        if (seasonSprites == null || index >= seasonSprites.Length || !seasonSprites[index])
        {
            Debug.LogWarning($"[TimeHUDController] No sprite for Season index {index} ({season}).");
            return;
        }
        seasonIconImage.sprite = seasonSprites[index];
    }
    #endregion
}