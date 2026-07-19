// Radial menu view: lays out option buttons in a ring with paging arrows and a hover tooltip.

using System;
using UnityEngine;
using UnityEngine.UIElements;

public class RadialMenuView
{
    private readonly VisualElement _overlay;
    private readonly VisualElement _ring;
    private readonly Label _tooltip;
    private readonly Button _prev, _next;
    private readonly Action<BuildOption> _onSelect;

    public RadialMenuView(VisualElement root, Action<BuildOption> onSelect, Action onPrev, Action onNext, Action onClose)
    {
        _overlay = root.Q<VisualElement>("radial-overlay");
        _ring = root.Q<VisualElement>("radial-ring");
        _tooltip = root.Q<Label>("radial-tooltip");
        _prev = root.Q<Button>("radial-prev");
        _next = root.Q<Button>("radial-next");
        _onSelect = onSelect;

        _overlay?.RegisterCallback<ClickEvent>(_ => onClose?.Invoke());
        _prev?.RegisterCallback<ClickEvent>(_ => onPrev?.Invoke());
        _next?.RegisterCallback<ClickEvent>(_ => onNext?.Invoke());
        HideTooltip();
    }

    public void SetVisible(bool visible)
    {
        if (_overlay != null)
            _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    public void SetCenter(Vector2 panelPos)
    {
        if (_ring == null)
            return;
        _ring.style.left = panelPos.x;
        _ring.style.top = panelPos.y;
    }
    
    public void RenderPage(System.Collections.Generic.List<BuildOption> page, bool hasPrev, bool hasNext, float radius)
    {
        if (_ring == null)
            return;
        
        _ring.Clear();

        var count = page.Count;
        for (var i = 0; i < count; i++)
        {
            var opt = page[i];
            var angle = (-90f + i * (360f / Mathf.Max(1, count))) * Mathf.Deg2Rad;
            var x = Mathf.Cos(angle) * radius;
            var y = Mathf.Sin(angle) * radius;

            var btn = new Button { text = opt.Type.ToString() };
            btn.AddToClassList("radial-option");
            if (!opt.Enabled) btn.AddToClassList("radial-option--disabled");
            btn.style.position = Position.Absolute;
            btn.style.left = x - 34f;
            btn.style.top = y - 24f; 

            if (opt.Enabled)
                btn.RegisterCallback<ClickEvent>(_ => _onSelect?.Invoke(opt));
            else
                btn.pickingMode = PickingMode.Ignore;

            btn.RegisterCallback<MouseEnterEvent>(_ => ShowTooltip(opt));
            btn.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip());
            _ring.Add(btn);
        }

        if (_prev != null)
            _prev.style.display = hasPrev ? DisplayStyle.Flex : DisplayStyle.None;
        if (_next != null)
            _next.style.display = hasNext ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ShowTooltip(BuildOption opt)
    {
        if (_tooltip == null)
            return;
        _tooltip.text = opt.Enabled
            ? $"{opt.Type}\nCost {opt.Cost}g  •  {opt.ConstructionHours}h"
            : $"{opt.Type}\n{opt.DisabledReason}";
        _tooltip.style.display = DisplayStyle.Flex;
    }

    private void HideTooltip()
    {
        if (_tooltip != null)
            _tooltip.style.display = DisplayStyle.None;
    }
}