// Spawns and animates a single floating gold-change label (+/- amount) that rises and fades out.

using UnityEngine;
using UnityEngine.UIElements;

public class GoldFloater
{
    private readonly VisualElement _layer;
    private readonly MonoBehaviour _runner;
    private readonly float _duration;
    private readonly float _riseDistance;

    public GoldFloater(VisualElement layer, MonoBehaviour runner, float duration, float riseDistance)
    {
        _layer = layer;
        _runner = runner;
        _duration = duration;
        _riseDistance = riseDistance;
    }
    
    public void Spawn(int delta)
    {
        if (_layer == null || delta == 0)
            return;

        var label = new Label(delta > 0 ? $"+{delta}" : delta.ToString());
        label.AddToClassList("gold-floater");
        label.AddToClassList(delta > 0 ? "gold-floater--gain" : "gold-floater--loss");
        label.style.position = Position.Absolute;
        label.style.left = Random.Range(0f, 12f);
        _layer.Add(label);

        _runner.StartCoroutine(UiTween.Run(_duration, t =>
        {
            var e = UiTween.EaseInOut(t);
            label.style.top = -_riseDistance * e;
            label.style.opacity = 1f - e;
        }, label.RemoveFromHierarchy));
    }
}