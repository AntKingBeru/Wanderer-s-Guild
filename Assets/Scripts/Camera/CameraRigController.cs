// Drives the camera rig: applies smoothed, bounds-clamped pan and continuous rotation from input intent.

using UnityEngine;

[RequireComponent(typeof(CameraInputReader))]
public class CameraRigController : MonoBehaviour
{
    [Tooltip("The rig root (this object). The camera is a child angled downward.")]
    [SerializeField] private CameraInputReader input;

    private Vector3 _targetPosition;
    private float _targetYaw;

    private void Awake()
    {
        _targetPosition = transform.position;
        _targetYaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        var config = GameConfig.Instance.Camera;
        
        var keyRot = input.IsDragRotating ? 0f : input.RotateIntent * config.rotateSpeed * Time.deltaTime;
        var dragRot = input.IsDragRotating ? input.RotateIntent : 0f;
        _targetYaw += keyRot + dragRot;

        var pan = input.PanIntent;
        var flatForward = Quaternion.Euler(0f, _targetYaw, 0f) * Vector3.forward;
        var flatRight = Quaternion.Euler(0f, _targetYaw, 0f) * Vector3.right;
        var move = (flatRight * pan.x + flatForward * pan.y) * (config.panSpeed * Time.deltaTime);
        
        _targetPosition += CameraPanBounds.Clamp(_targetPosition + move, config.panMin, config.panMax);
        
        var posK = 1f - Mathf.Exp(-config.moveSmoothing * Time.deltaTime);
        var rotK = 1f - Mathf.Exp(-config.rotateSmoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, _targetPosition, posK);
        
        var smoothedYaw = Mathf.LerpAngle(transform.eulerAngles.y, _targetYaw, rotK);
        transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
    }
}