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
    
    [Tooltip("World-unit padding added to each side of the combined room footprint. " +
             "The rig pivot is clamped so the camera can never pan beyond rooms + this offset.")]
    [SerializeField, Min(0f)] private float panBoundsOffset = 25f;
    
    [Tooltip("Minimum half-extent of the pan bounds on each axis when no rooms exist yet. " +
             "Keeps the camera from being locked to origin before any room is placed.")]
    [SerializeField, Min(1f)] private float defaultBoundsHalfExtent = 25f;
    
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
    
    // Computed pan bounds on the XZ plane; updated when rooms change.
    private float _minX, _maxX, _minZ, _maxZ;
    
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
        
        // Start with default bounds so the camera is usable before any rooms are placed.
        ApplyDefaultBounds();
    }

    private void OnEnable()
    {
        EnableAction(panAction);
        EnableAction(panButtonAction);
        EnableAction(rotateAction);
        EnableAction(rotateButtonAction);
        EnableAction(zoomAction);
        
        // Recompute bounds whenever a room is added, removed, or completed.
        if (GameEventRelay.Instance)
            GameEventRelay.Instance.OnRoomsChanged.AddListener(RefreshBounds);
    }
    
    private void OnDisable()
    {
        DisableAction(panAction);
        DisableAction(panButtonAction);
        DisableAction(rotateAction);
        DisableAction(rotateButtonAction);
        DisableAction(zoomAction);

        if (GameEventRelay.Instance)
            GameEventRelay.Instance.OnRoomsChanged.RemoveListener(RefreshBounds);
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
        if (!panButtonAction.action.IsPressed()) return;

        var delta = panAction.action.ReadValue<Vector2>();
        if (delta == Vector2.zero) return;

        var move = new Vector3(-delta.x, 0f, -delta.y);
        var distanceFactor = transform.localPosition.magnitude / 10f;
        move *= panSpeed * distanceFactor;
        move = Quaternion.Euler(0f, _rig.eulerAngles.y, 0f) * move;

        var newPos = _rig.position + move;

        // Clamp to the computed room bounds on the XZ plane; Y stays at 0.
        newPos.x = Mathf.Clamp(newPos.x, _minX, _maxX);
        newPos.z = Mathf.Clamp(newPos.z, _minZ, _maxZ);
        newPos.y = 0f;

        _rig.position = newPos;
    }
    #endregion
    
    #region Orbit
    private void HandleRotation()
    {
        if (!rotateButtonAction.action.IsPressed()) return;

        var delta = rotateAction.action.ReadValue<Vector2>();
        if (delta == Vector2.zero) return;

        _yaw   += delta.x * rotateSpeed;
        _pitch -= delta.y * rotateSpeed;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

        _rig.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
    #endregion
    
    #region Zoom
    private void HandleZoom()
    {
        var scroll = zoomAction.action.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetZoomDistance -= scroll * zoomSpeed;
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance, minZoomDistance, maxZoomDistance);
        }

        var currentDistance = transform.localPosition.magnitude;
        var smoothedDistance = Mathf.SmoothDamp(
            currentDistance, _targetZoomDistance, ref _zoomVelocity, zoomSmoothTime);
        transform.localPosition = transform.localPosition.normalized * smoothedDistance;
    }
    #endregion
    
    #region Pan Bounds
    // Called automatically via OnRoomsChanged; also safe to call manually from editor tooling.
    public void RefreshBounds()
    {
        if (!BuildManager.Instance || BuildManager.Instance.Rooms.Count == 0)
        {
            ApplyDefaultBounds();
            return;
        }

        // Rooms have no world-space transform yet (build system places them as data only).
        // Expand the bounds by panBoundsOffset for each room beyond the first, so the camera
        // can reach progressively further as the guild grows.
        // TODO: replace with actual room footprint once rooms have world-space GameObjects.
        var roomCount = BuildManager.Instance.Rooms.Count;
        var expansion = panBoundsOffset + (roomCount - 1) * 10f;

        _minX = -expansion;
        _maxX = expansion;
        _minZ = -expansion;
        _maxZ = expansion;
    }

    private void ApplyDefaultBounds()
    {
        _minX = -defaultBoundsHalfExtent;
        _maxX = defaultBoundsHalfExtent;
        _minZ = -defaultBoundsHalfExtent;
        _maxZ = defaultBoundsHalfExtent;
    }

    private static void EnableAction(InputActionReference r)
    {
        if (r)
            r.action.Enable();
    }

    private static void DisableAction(InputActionReference r)
    {
        if (r)
            r.action.Disable();
    }
    #endregion
}