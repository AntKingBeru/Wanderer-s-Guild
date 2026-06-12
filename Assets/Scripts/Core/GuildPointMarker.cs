// Attach to any world prop that should serve as a destination for adventurers.
// Self-registers with GuildPointsRegistry on Awake so no manual wiring is needed.
// Set the Type field in the inspector to ReceptionDesk, GuildBoard, or Exit.

using UnityEngine;

public class GuildPointMarker : MonoBehaviour
{
    [Tooltip("What kind of destination this prop represents.")]
    [SerializeField] private GuildPointType type = GuildPointType.ReceptionDesk;

    [Tooltip("The exact standing position adventurers will walk to. " +
             "Defaults to this object's transform if left empty.")]
    [SerializeField] private Transform standPoint;
    
    // The point exposed to the registry — standPoint if assigned, otherwise this transform.
    public Transform StandTransform => standPoint ? standPoint : transform;
    
    private void OnEnable()
    {
        if (GuildPointRegistry.Instance)
            GuildPointRegistry.Instance.RegisterPoint(type, StandTransform);
        else
            Debug.LogError("[GuildPointMarker] GuildPointsRegistry not found. " +
                           "Ensure it is in the scene and has an earlier Script Execution Order.");
    }

    private void OnDestroy()
    {
        if (GuildPointRegistry.Instance)
            GuildPointRegistry.Instance.UnregisterPoint(type, StandTransform);
    }
}