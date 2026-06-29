// Reusable UI-Toolkit drag helper: pointer-captures a source, shows a ghost, reports the drop target.

using System;
using UnityEngine;
using UnityEngine.UIElements;

public class DragController
{
    private readonly VisualElement _dragLayer;
    private readonly Func<int, VisualElement> _ghostFactory;
    private readonly Action<int, Vector2> _onDrop;

    private VisualElement _ghost;
    private int _payloadId = -1;
    private VisualElement _captured;

    public bool IsDragging => _payloadId != -1;
    
    public DragController(VisualElement dragLayer, Func<int, VisualElement> ghostFactory,
        Action<int, Vector2> onDrop)
    {
        _dragLayer = dragLayer;
        _ghostFactory = ghostFactory;
        _onDrop = onDrop;
        _dragLayer.pickingMode = PickingMode.Ignore;
    }
    
    public void MakeDraggable(VisualElement source, int payloadId)
    {
        source.RegisterCallback<PointerDownEvent>(e => BeginDrag(e, source, payloadId));
    }

    private void BeginDrag(PointerDownEvent e, VisualElement source, int payloadId)
    {
        if (e.button != 0 || IsDragging) return;

        _payloadId = payloadId;
        _captured = source;
        source.CapturePointer(e.pointerId);

        _ghost = _ghostFactory(payloadId);
        if (_ghost != null)
        {
            _ghost.pickingMode = PickingMode.Ignore;
            _ghost.style.position = Position.Absolute;
            _dragLayer.Add(_ghost);
            MoveGhost(e.position);
        }

        source.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        source.RegisterCallback<PointerUpEvent>(OnPointerUp);
        source.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        e.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (!IsDragging)
            return;
        MoveGhost(e.position);
    }

    private void OnPointerUp(PointerUpEvent e)
    {
        if (!IsDragging)
            return;
        var payload = _payloadId;
        Vector2 pos = e.position;

        _captured?.ReleasePointer(e.pointerId);
        _onDrop?.Invoke(payload, pos);
    }
    
    private void OnCaptureOut(PointerCaptureOutEvent _)
    {
        if (_captured != null)
        {
            _captured.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _captured.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _captured.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        }

        if (_ghost != null)
        {
            _ghost.RemoveFromHierarchy();
            _ghost = null;
        }
        _payloadId = -1;
        _captured = null;
    }
    
    private void MoveGhost(Vector2 screenPos)
    {
        if (_ghost == null)
            return;
        _ghost.style.left = screenPos.x - _ghost.resolvedStyle.width / 2f;
        _ghost.style.top = screenPos.y - _ghost.resolvedStyle.height / 2f;
    }
}