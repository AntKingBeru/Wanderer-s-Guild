// Rotates its transform each frame to face the active camera (billboard effect).

using UnityEngine;

public class BillboardFollow : MonoBehaviour
{
    private Transform _cam;

    private void Start()
    {
        if (Camera.main)
            _cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        transform.forward = transform.position - _cam.position;
    }
}