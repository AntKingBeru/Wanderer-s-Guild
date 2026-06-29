// Pure HUD view: holds time elements and exposes setters. No event/sim knowledge.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeHudView
{
    private readonly VisualElement _seasonIcon;
    private readonly Label _dateLabel;
    private readonly Label _yearLabel;
    private readonly Label _speedBadge;
    private readonly Dictionary<TimeSpeed, Button> _speedButtons;
    
    public TimeHudView(VisualElement root)
    {
        _seasonIcon = root.Q<VisualElement>("season-icon");
        _dateLabel  = root.Q<Label>("date-label");
        _yearLabel  = root.Q<Label>("year-label");
        _speedBadge = root.Q<Label>("speed-badge");
        _speedButtons = new Dictionary<TimeSpeed, Button>
        {
            { TimeSpeed.Pause, root.Q<Button>("btn-pause") },
            { TimeSpeed.Normal, root.Q<Button>("btn-normal") },
            { TimeSpeed.Fast, root.Q<Button>("btn-fast") },
            { TimeSpeed.VeryFast, root.Q<Button>("btn-very-fast") },
        };
    }
    
    public Button GetSpeedButton(TimeSpeed speed) =>
        _speedButtons.GetValueOrDefault(speed);
    
    public void SetSeasonIcon(Sprite icon)
    {
        if (_seasonIcon == null || !icon)
            return;
        _seasonIcon.style.backgroundImage = new StyleBackground(icon);
    }
    
    public void SetDate(GameDate date)
    {
        if (_dateLabel != null)
            _dateLabel.text = $"Day {date.day}";
        if (_yearLabel != null)
            _yearLabel.text = $"Year {date.year}";
    }
    
    public void SetSpeed(TimeSpeed speed, Color color)
    {
        if (_speedBadge != null)
        {
            _speedBadge.text = SpeedLabel(speed);
            _speedBadge.style.backgroundColor = color;
        }
        foreach (var kvp in _speedButtons)
            kvp.Value?.EnableInClassList("speed-btn--active", kvp.Key == speed);
    }
    
    private static string SpeedLabel(TimeSpeed s) => s switch
    {
        TimeSpeed.Pause => "PAUSED",
        TimeSpeed.Normal => "NORMAL",
        TimeSpeed.Fast => "FAST",
        TimeSpeed.VeryFast => "VERY FAST",
        _ => s.ToString()
    };
}