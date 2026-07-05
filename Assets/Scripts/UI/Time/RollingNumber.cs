// Animates a single number "rolling" upward within a clipped container (old slides out, new slides in).

using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class RollingNumber
{
    private readonly VisualElement _clip;
    private readonly Label _current;
    private Label _incoming;
    private readonly MonoBehaviour _runner;
    private Coroutine _active;
    private int _value;

    public RollingNumber(VisualElement clip, Label current, MonoBehaviour runner)
    {
        _clip = clip;
        _current = current;
        _runner = runner;
        
        _current.style.position = Position.Absolute;
        _current.style.left = 0;
        _current.style.right = 0;
        _current.style.unityTextAlign = TextAnchor.MiddleCenter;
    }

    public void SetImmediate(int value, string format)
    {
        _value = value;
        _current.text = string.Format(format, value);
        CenterVertically(_current, 0f);
    }

    public void RollTo(int newValue, string format, float duration)
    {
        if (newValue == _value && _incoming == null)
            return;
        if (_active != null)
            _runner.StopCoroutine(_active);
        FinishPending();
        
        _value = newValue;
        
        _incoming = new Label(string.Format(format, newValue));
        foreach (var cls in _current.GetClasses())
            _incoming.AddToClassList(cls);
        _incoming.style.position = Position.Absolute;
        _incoming.style.left = 0;
        _incoming.style.right = 0;
        _incoming.style.unityTextAlign = TextAnchor.MiddleCenter;
        _clip.Add(_incoming);
        
        var h = Mathf.Max(1f, _clip.resolvedStyle.height);
        _active = _runner.StartCoroutine(WaitForLayoutThenRoll(h, duration));
    }
    
    private IEnumerator WaitForLayoutThenRoll(float clipHeight, float duration)
    {
        yield return null;

        var labelH = Mathf.Max(1f, _incoming.resolvedStyle.height);
        var centerOffset = (clipHeight - labelH) * 0.5f; 

        _active = _runner.StartCoroutine(UiTween.Run(duration, t =>
        {
            var e = UiTween.EaseInOut(t);
            _current.style.top = centerOffset - clipHeight * e;
            _incoming.style.top = centerOffset + clipHeight * (1f - e);
        }, OnRollComplete));
    }

    private void OnRollComplete()
    {
        _current.text = _incoming.text;
        FinishPending();
        CenterVertically(_current, 0f);
        _active = null;
    }

    private void FinishPending()
    {
        if (_incoming != null)
        {
            _incoming.RemoveFromHierarchy();
            _incoming = null;
        }
        _current.style.top = 0;
    }
    
    private void CenterVertically(Label label, float extra)
    {
        var clipH = Mathf.Max(1f, _clip.resolvedStyle.height);
        var labelH = Mathf.Max(1f, label.resolvedStyle.height);
        label.style.top = (clipH - labelH) * 0.5f + extra;
    }
}