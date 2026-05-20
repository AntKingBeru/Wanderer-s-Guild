using UnityEngine;
using UnityEngine.InputSystem;

public class IsoCamController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference panAction;
    [SerializeField] private InputActionReference panButtonAction;
    [SerializeField] private InputActionReference rotateAction;
    [SerializeField] private InputActionReference rotateButtonAction;
    [SerializeField] private InputActionReference zoomAction;
    
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 0.02f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotateSpeed = 0.3f;
    [SerializeField] private float minPitch = 10f;
    [SerializeField] private float maxPitch = 85f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoomDistance = 3f;
    [SerializeField] private float maxZoomDistance = 40f;
    [SerializeField] private float zoomSmoothTime = 0.15f;

    // The parent pivot for the camera rig
    private Transform _rig;
    
    // Euler angles we own, so we never fight gimbal drift
    private float _yaw;
    private float _pitch;
    
    // Smooth zoom state
    private float _targetZoomDistance;
    private float _zoomVelocity;
    
    #region Setup
    private void Awake()
    {
        // Camera must bt a child of the rig
        _rig = transform.parent;
        Debug.Assert(_rig, "Isometric Camera Controller: Camera must be a child game object CameraRig.");
        
        // Seed owned angles from the rig's current rotation so the inspector-set starting angle is respected
        var rigEuler = _rig.eulerAngles;
        _yaw = rigEuler.y;
        _pitch = rigEuler.x;
        
        // Seed zoom from the camera's current local position magnitude
        _targetZoomDistance = transform.localPosition.magnitude;
    }

    private void OnEnable()
    {
        panAction.action.Enable();
        panButtonAction.action.Enable();
        rotateAction.action.Enable();
        rotateButtonAction.action.Enable();
        zoomAction.action.Enable();
    }
    
    private void OnDisable()
    {
        panAction.action.Disable();
        panButtonAction.action.Disable();
        rotateAction.action.Disable();
        rotateButtonAction.action.Disable();
        zoomAction.action.Disable();
    }
    #endregion

    private void Update()
    {
        HandlePan();
        HandleRotation();
        HandleZoom();
    }
    
    #region Pan
    private void HandlePan()
    {
        if (!panButtonAction.action.IsPressed())
            return;
        
        var delta = panAction.action.ReadValue<Vector2>();
        if (delta == Vector2.zero)
            return;
        
        // Build a pan vector on the rig's local XZ plane.
        // Mouse moving right -> scene moves right -> camera moves LEFT (negate X)
        // Mouse moving up -> scene moves up -> camera moves DOWN (negate Y->Z)
        var move = new Vector3(-delta.x, 0f, -delta.y);
        
        // Scale by distance so panning feels consistent at any zoom level
        var distanceFactor = transform.localPosition.magnitude / 10f;
        move *= panSpeed * distanceFactor;
        
        // Transform from rig-local space to world space (ignore rig pitch so panning always stays on the ground plane)
        move = Quaternion.Euler(0f, _rig.eulerAngles.y, 0f) * move;
        
        _rig.position += move;
    }
    #endregion
    
    #region Orbit
    private void HandleRotation()
    {
        if (!rotateButtonAction.action.IsPressed())
            return;
        
        var delta = rotateAction.action.ReadValue<Vector2>();
        if (delta == Vector2.zero)
            return;
        
        _yaw += delta.x * rotateSpeed;
        _pitch -= delta.y * rotateSpeed;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        
        _rig.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
    #endregion
    
    #region Zoom
    private void HandleZoom()
    {
        var scroll = zoomAction.action.ReadValue<float>();

        if (Mathf.Abs(scroll) > 0.01f)
        {
            // scroll > 0 -> zoom in -> reduce distance
            _targetZoomDistance -= scroll * zoomSpeed;
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance, minZoomDistance, maxZoomDistance);
        }
        
        // Smooth damp towards target distance along local -Z (camera looks down -Z)
        var currentDistance = transform.localPosition.magnitude;
        var smoothedDistance = Mathf.SmoothDamp(
            currentDistance, _targetZoomDistance, ref _zoomVelocity, zoomSmoothTime);
        
        // Preserve local direction, only change magnitude
        transform.localPosition = transform.localPosition.normalized * smoothedDistance;
    }
    #endregion
}