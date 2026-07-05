// Reads camera input (keys, screen-edge, middle-drag) via InputActionReferences into pan/rotate intent.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CameraInputReader : MonoBehaviour
{
    [Header("Input Action References")]
    [Tooltip("Vector2 'Value' action for keyboard pan (WASD/arrows).")]
    [SerializeField] private InputActionReference panKeys;
    [Tooltip("Button held to drag-pan (middle mouse).")]
    [SerializeField] private InputActionReference dragPan;
    [Tooltip("Button held to drag-rotate (e.g. right mouse), or use _rotateKeys.")]
    [SerializeField] private InputActionReference dragRotate;
    [Tooltip("Axis (float) for key rotation (e.g. Q/E as -1/+1).")]
    [SerializeField] private InputActionReference rotateKeys;
    [Tooltip("Vector2 'Value' action for pointer position.")]
    [SerializeField] private InputActionReference pointerPosition;
    
    [Header("Panel Reference")]
    [Tooltip("UIDocument whose panel defines the coordinate space for edge detection.")]
    [SerializeField] private UIDocument referenceDocument;
    
    public Vector2 PanIntent { get; private set; }
    public float RotateIntent { get; private set; }
    
    private Vector2 _lastPointer;
    private bool _dragPanning;
    private bool _dragRotating;
    private bool _pointerPrimed;
    
    public bool IsDragRotating => _dragRotating;
    
    private void OnEnable()
    {
        Enable(panKeys);
        Enable(dragPan);
        Enable(dragRotate);
        Enable(rotateKeys);
        Enable(pointerPosition);
    }
    
    private void Update()
    {
        var screenPointer = pointerPosition?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        
        if (screenPointer != Vector2.zero)
            _pointerPrimed = true;

        PanIntent = ReadPan(screenPointer);
        RotateIntent = ReadRotate(screenPointer);

        _lastPointer = screenPointer;
    }

    private void Enable(InputActionReference r)
    {
        r?.action?.Enable();
    }
    
    private Vector2 ReadPan(Vector2 screenPointer)
    {
        var intent = panKeys?.action?.ReadValue<Vector2>() ?? Vector2.zero;

        var dragHeld = dragPan?.action != null && dragPan.action.IsPressed();
        if (dragHeld)
        {
            if (!_dragPanning)
                _dragPanning = true;
            else
            {
                var delta = screenPointer - _lastPointer;   // screen-space delta is fine for drag
                intent += new Vector2(-delta.x, -delta.y) * GameConfig.Instance.Camera.dragPanSpeed;
            }
        }
        else
            _dragPanning = false;

        if (!dragHeld)
            intent += EdgePan(screenPointer);
        return intent;
    }
    
    private Vector2 EdgePan(Vector2 screenPointer)
    {
        if (!_pointerPrimed || !Application.isFocused ||
            screenPointer.x < 0f || screenPointer.y < 0f ||
            screenPointer.x > Screen.width || screenPointer.y > Screen.height)
            return Vector2.zero;

        var panel = referenceDocument ? referenceDocument.rootVisualElement?.panel : null;
        if (panel == null)
            return Vector2.zero;
        
        var p = RuntimePanelUtils.ScreenToPanel(panel, screenPointer);
        var size = referenceDocument.rootVisualElement.layout.size;
        if (size.x <= 0f || size.y <= 0f)
            return Vector2.zero;

        var border = GameConfig.Instance.Camera.edgePanBorder;
        var e = Vector2.zero;

        if (p.x <= border)
            e.x -= 1f;
        else if (p.x >= size.x - border)
            e.x += 1f;
        
        if (p.y <= border)
            e.y += 1f;
        else if (p.y >= size.y - border)
            e.y -= 1f;

        return e;
    }
    
    private float ReadRotate(Vector2 screenPointer)
    {
        var intent = rotateKeys?.action?.ReadValue<float>() ?? 0f;

        var rotHeld = dragRotate?.action != null && dragRotate.action.IsPressed();
        if (rotHeld)
        {
            if (!_dragRotating)
                _dragRotating = true;
            else
                intent += (screenPointer.x - _lastPointer.x) * GameConfig.Instance.Camera.dragRotateSpeed;
        }
        else
            _dragRotating = false;

        return intent;
    }
}