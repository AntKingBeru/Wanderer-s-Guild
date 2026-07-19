// Drives a billboard HP bar's fill width from a 0..1 health ratio.

using UnityEngine.UIElements;
using UnityEngine;

[RequireComponent(typeof(UIDocument))]
public class BillboardHealthBar : MonoBehaviour
{
    private VisualElement _fill;

    private void Awake()
        => _fill = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("hp-fill");
    
    public void SetRatio(float ratio)
    {
        if (_fill == null)
            return;
        _fill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
    }
}