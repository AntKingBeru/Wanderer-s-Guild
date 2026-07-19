// Drives the camera rig: applies smoothed, bounds-clamped pan and continuous rotation from input intent.

using UnityEngine;

[RequireComponent(typeof(CameraInputReader))]
public class CameraRigController : MonoBehaviour
{
    [Tooltip("The rig root (this object). The camera is a child angled downward.")]
    [SerializeField] private CameraInputReader input;

    [SerializeField] private CameraZoom zoom;

    private Vector3 _targetPosition;
    private Vector3 _currentPosition;
    private float _targetYaw;
    private float _currentYaw;


    private void Awake()
    {
        _currentPosition = _targetPosition = transform.position;
        _currentYaw = _targetYaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        var config = GameConfig.Instance.Camera;
        
        var keyRot = input.IsDragRotating ? 0f : input.RotateIntent * config.rotateSpeed * Time.deltaTime;
        var dragRot = input.IsDragRotating ? input.RotateIntent : 0f;
        _targetYaw += keyRot + dragRot;
        
        var pan = input.PanIntent;
        var yawRot = Quaternion.Euler(0f, _targetYaw, 0f);
        var zoomMult = zoom ? zoom.PanSpeedMultiplier : 1f;
        var move = (yawRot * Vector3.right * pan.x + yawRot * Vector3.forward * pan.y) * (config.panSpeed * zoomMult * Time.deltaTime);
        _targetPosition = CameraPanBounds.Clamp(_targetPosition + move, config.panMin, config.panMax);
        
        var posK = 1f - Mathf.Exp(-config.moveSmoothing * Time.deltaTime);
        var rotK = 1f - Mathf.Exp(-config.rotateSmoothing * Time.deltaTime);

        _currentPosition = Vector3.Lerp(_currentPosition, _targetPosition, posK);
        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, rotK);
        
        transform.position = _currentPosition;
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
    }
}