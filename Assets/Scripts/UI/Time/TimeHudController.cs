// HUD presenter: binds the time view, wires speed buttons, and syncs from time events.

using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(10)]
public class TimeHudController : MonoBehaviour
{
    [Header("Season Icons — order must match: Spring, Summer, Autumn, Winter")]
    [SerializeField] private Sprite[] seasonIcons = new Sprite[4];
    
    [Header("Clock Readout")]
    [Tooltip("Seconds between HH:MM refreshes (real time).")]
    [SerializeField] private float baseClockRefresh = 0.25f;
    [Tooltip("Minimum refresh interval, so very fast speeds don't refresh every single frame.")]
    [SerializeField] private float minClockRefresh = 0.03f;

    [Header("Time-Scale Colours")]
    [SerializeField] private Color pauseColor = new(0.55f, 0.55f, 0.60f);
    [SerializeField] private Color normalColor = new(0.30f, 0.72f, 0.40f);
    [SerializeField] private Color fastColor = new(0.92f, 0.74f, 0.26f);
    [SerializeField] private Color veryFastColor = new(0.86f, 0.33f, 0.28f);
    
    [Header("Animation")]
    [SerializeField] private float rollDuration = 0.35f;
    [SerializeField] private float seasonFadeDuration = 0.6f;
    
    private TimeHudView _view;
    private RollingNumber _dateRoll;
    private RollingNumber _yearRoll;
    private Coroutine _seasonFade;
    private float _clockRefreshTimer;
    private int _lastYear = -1;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[TimeHud] UIDocument root not ready.");
            return;
        }
        
        _view = new TimeHudView(root);
        _dateRoll = new RollingNumber(_view.DateClip, _view.DateLabel, this);
        _yearRoll = new RollingNumber(_view.YearClip, _view.YearLabel, this);
        WireButtons();
        Subscribe();
        SyncFromController();
    }

    private void OnDisable()
        => Unsubscribe();
    
    private void Update()
    {
        if (!TimeController.Exists || _view == null)
            return;
        _clockRefreshTimer -= Time.unscaledDeltaTime;
        if (_clockRefreshTimer > 0f)
            return;
        
        var mult = TimeController.Instance.CurrentSpeedMultiplier;
        var interval = mult <= 0f
            ? baseClockRefresh
            : Mathf.Max(minClockRefresh, baseClockRefresh / mult);
        _clockRefreshTimer = interval;
        
        _view.SetTimeOfDay(TimeController.Instance.Hour, TimeController.Instance.Minute);
    }

    private void WireButtons()
    {
        _view.GetSpeedButton(TimeSpeed.Pause)?.RegisterCallback<ClickEvent>(_ => TimeController.Instance.SetSpeed(TimeSpeed.Pause));
        _view.GetSpeedButton(TimeSpeed.Normal)?.RegisterCallback<ClickEvent>(_ => TimeController.Instance.SetSpeed(TimeSpeed.Normal));
        _view.GetSpeedButton(TimeSpeed.Fast)?.RegisterCallback<ClickEvent>(_ => TimeController.Instance.SetSpeed(TimeSpeed.Fast));
        _view.GetSpeedButton(TimeSpeed.VeryFast)?.RegisterCallback<ClickEvent>(_ => TimeController.Instance.SetSpeed(TimeSpeed.VeryFast));
    }
    
    private void Subscribe()
    {
        var relay = GameEventsRelay.Instance;
        relay.onDayAdvanced.AddListener(HandleDate);
        relay.onSeasonChanged.AddListener(HandleSeason);
        relay.onTimeSpeedChanged.AddListener(HandleSpeed);
    }
    
    private void Unsubscribe()
    {
        if (!GameEventsRelay.Exists) return;
        var relay = GameEventsRelay.Instance;
        relay.onDayAdvanced.RemoveListener(HandleDate);
        relay.onSeasonChanged.RemoveListener(HandleSeason);
        relay.onTimeSpeedChanged.RemoveListener(HandleSpeed);
    }
    
    private void SyncFromController()
    {
        if (!TimeController.Exists)
            return;
        var tc = TimeController.Instance;
        _view.SetTimeOfDay(tc.Hour, tc.Minute);
        HandleSeason(tc.CurrentDate.season);
        HandleDate(tc.CurrentDate);
        HandleSpeed(tc.CurrentSpeed);
    }
    
    private void HandleDate(GameDate date)
    {
        _dateRoll.RollTo(date.day, "Day {0}", rollDuration);
        if (date.year != _lastYear)
        {
            _yearRoll.RollTo(date.year, "Year {0}", rollDuration);
            _lastYear = date.year;
        }
    }

    private void HandleSeason(Season season)
    {
        var i = (int)season;
        var icon = i >= 0 && i < seasonIcons.Length ? seasonIcons[i] : null;
        if (!icon)
            return;
        
        if (_view.SeasonIcon.resolvedStyle.backgroundImage == null)
        {
            _view.SetSeasonIcon(icon);
            return;
        }

        _view.SetSeasonIconNext(icon);
        if (_seasonFade != null)
            StopCoroutine(_seasonFade);
        _seasonFade = StartCoroutine(UiTween.Run(seasonFadeDuration, t =>
        {
            var e = UiTween.EaseInOut(t);
            _view.SeasonIconNext.style.opacity = e;
        }, () =>
        {
            _view.SetSeasonIcon(icon);
            _view.SeasonIconNext.style.opacity = 0;
            _seasonFade = null;
        }));
    }
    
    private void HandleSpeed(TimeSpeed speed)
        => _view?.SetSpeed(speed, ColorFor(speed));
    
    private Color ColorFor(TimeSpeed s) => s switch
    {
        TimeSpeed.Pause => pauseColor,
        TimeSpeed.Normal => normalColor,
        TimeSpeed.Fast => fastColor,
        TimeSpeed.VeryFast => veryFastColor,
        _ => normalColor
    };
}