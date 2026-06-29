// HUD presenter: binds the time view, wires speed buttons, and syncs from time events.

using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(10)]
public class TimeHudController : MonoBehaviour
{
    [Header("Season Icons — order must match: Spring, Summer, Autumn, Winter")]
    [SerializeField] private Sprite[] seasonIcons = new Sprite[4];

    [Header("Time-Scale Colours")]
    [SerializeField] private Color pauseColor = new(0.55f, 0.55f, 0.60f);
    [SerializeField] private Color normalColor = new(0.30f, 0.72f, 0.40f);
    [SerializeField] private Color fastColor = new(0.92f, 0.74f, 0.26f);
    [SerializeField] private Color veryFastColor = new(0.86f, 0.33f, 0.28f);
    
    private TimeHudView _view;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[TimeHud] UIDocument root not ready.");
            return;
        }
        
        _view = new TimeHudView(root);
        WireButtons();
        Subscribe();
        SyncFromController();
    }

    private void OnDisable()
        => Unsubscribe();

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
        HandleSeason(tc.CurrentDate.season);
        HandleDate(tc.CurrentDate);
        HandleSpeed(tc.CurrentSpeed);
    }
    
    private void HandleDate(GameDate date)
        => _view?.SetDate(date);

    private void HandleSeason(Season season)
    {
        var i = (int)season;
        var icon = i >= 0 && i < seasonIcons.Length
            ? seasonIcons[i]
            : null;
        _view?.SetSeasonIcon(icon);
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