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
    private VisualElement _captured;
    private int _payloadId = -1;
    private int _pointerId = -1;

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
        if (e.button != 0 || IsDragging)
            return;

        _payloadId = payloadId;
        _captured = source;
        _pointerId = e.pointerId;
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

        EndDrag();
        
        _onDrop?.Invoke(payload, pos);
    }
    
    private void EndDrag()
    {
        if (_captured != null)
        {
            if (_pointerId != -1 && _captured.HasPointerCapture(_pointerId))
                _captured.ReleasePointer(_pointerId);
            _captured.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _captured.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        if (_ghost != null)
        {
            _ghost.RemoveFromHierarchy();
            _ghost = null;
        }
        _captured = null;
        _payloadId = -1;
        _pointerId = -1;
    }
    
    private void MoveGhost(Vector2 screenPos)
    {
        if (_ghost == null)
            return;
        _ghost.style.left = screenPos.x - _ghost.resolvedStyle.width / 2f;
        _ghost.style.top = screenPos.y - _ghost.resolvedStyle.height / 2f;
    }
}