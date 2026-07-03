// Trigger-collider door: swings open away from an entering adventurer and closes when clear.

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerDoor : MonoBehaviour
{
    [Header("Hinge")]
    [Tooltip("The pivoting door transform (child). This object holds the trigger.")]
    [SerializeField] private Transform hinge;
    
    private Quaternion _closedRot;
    private Quaternion _targetRot;
    private int _occupants;
    
    private void Awake()
    {
        if (!hinge)
            hinge = transform;
        _closedRot = hinge.localRotation;
        _targetRot = _closedRot;
    }
    
    private void Update()
    {
        var speed = GameConfig.Instance.World.doorSpeed;
        hinge.localRotation = Quaternion.Slerp(hinge.localRotation, _targetRot, Time.deltaTime * speed);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out AdventurerMovement _))
            return;
        _occupants++;
        OpenAwayFrom(other.transform.position);
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out AdventurerMovement _))
            return;
        _occupants = Mathf.Max(0, _occupants - 1);
        if (_occupants == 0) _targetRot = _closedRot;
    }
    
    private void OpenAwayFrom(Vector3 enterPos)
    {
        var angle = GameConfig.Instance.World.doorOpenAngle;
        var toEntrant = enterPos - hinge.position;
        var side = Vector3.Dot(hinge.forward, toEntrant);
        var signedAngle = side > 0f ? -angle : angle;
        _targetRot = _closedRot * Quaternion.Euler(0f, signedAngle, 0f);
    }
}