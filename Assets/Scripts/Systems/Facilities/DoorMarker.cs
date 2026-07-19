// Data holder on a spawned door-highlight marker: the door it represents (raycast-selected in build mode).

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorMarker : MonoBehaviour
{
    public DoorKey Door { get; private set; }
    public Vector3 WorldPosition { get; private set; }

    public void Bind(DoorKey door, Vector3 worldPos)
    {
        Door = door;
        WorldPosition = worldPos;
    }
}