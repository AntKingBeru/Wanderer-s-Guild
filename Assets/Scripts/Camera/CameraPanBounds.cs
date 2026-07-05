// Pure helper: clamps a rig world position to the configured XZ pan bounds.

using UnityEngine;

public static class CameraPanBounds
{
    public static Vector3 Clamp(Vector3 position, Vector2 min, Vector2 max)
    {
        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.z = Mathf.Clamp(position.z, min.y, max.y);
        return position;
    }
}