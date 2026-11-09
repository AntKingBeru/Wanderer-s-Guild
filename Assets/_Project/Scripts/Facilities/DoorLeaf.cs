// A door leaf that swings on its hinge: open while an adventurer is near (DoorSwingController decides), closed otherwise.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Put it on the leaf (or its hinge pivot). Registers only while enabled — a leaf inside a disabled doorway neither
    // swings nor counts — and resets to closed when disabled.
    [DisallowMultipleComponent]
    public class DoorLeaf : MonoBehaviour
    {
        private static readonly List<DoorLeaf> Active = new();

        [Tooltip("The transform that rotates (its pivot on the hinge). Empty = this transform.")]
        [SerializeField] private Transform hinge;
        [Tooltip("Open angle around the hinge's up axis, in degrees (negative swings the other way).")]
        [SerializeField] private float openAngle = 90f;
        [Tooltip("Where adventurers are measured from (the doorway's centre is best). Empty = the hinge.")]
        [SerializeField] private Transform sensePoint;

        private Quaternion _closed;
        private float _angle;
        private bool _hasClosed;

        public static IReadOnlyList<DoorLeaf> All => Active;

        public Vector3 SensePosition => (sensePoint ? sensePoint : Hinge).position;

        private Transform Hinge => hinge ? hinge : transform;

        private void Awake()
        {
            _closed = Hinge.localRotation;
            _hasClosed = true;
        }

        private void OnEnable() => Active.Add(this);

        private void OnDisable()
        {
            Active.Remove(this);
            _angle = 0f;
            Apply();
        }

        // Moves toward open or closed at the given speed (degrees per second).
        public void Step(bool open, float degreesPerSecond, float deltaTime)
        {
            var target = open ? openAngle : 0f;
            if (Mathf.Approximately(_angle, target))
                return;

            _angle = Mathf.MoveTowards(_angle, target, degreesPerSecond * deltaTime);
            Apply();
        }

        private void Apply()
        {
            if (_hasClosed)
                Hinge.localRotation = _closed * Quaternion.Euler(0f, _angle, 0f);
        }
    }
}