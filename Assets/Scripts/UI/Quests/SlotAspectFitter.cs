// Keeps board slots at a fixed aspect ratio by deriving each slot's height from its resolved width.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SlotAspectFitter
{
    private readonly float _aspect;
    private readonly List<VisualElement> _slots = new List<VisualElement>();

    public SlotAspectFitter(float heightOverWidth = 0.7f)
        => _aspect = heightOverWidth;
    
    public void Track(VisualElement slot)
    {
        _slots.Add(slot);
        slot.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }
    
    public void Clear()
    {
        foreach (var slot in _slots)
            slot.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        _slots.Clear();
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        var slot = (VisualElement)evt.target;
        var width = slot.resolvedStyle.width;
        if (width <= 0f) return;

        var targetHeight = width * _aspect;
        if (Mathf.Abs(slot.resolvedStyle.height - targetHeight) > 0.5f)
            slot.style.height = targetHeight;
    }
}