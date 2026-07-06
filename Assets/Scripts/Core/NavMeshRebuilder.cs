// Observer that asynchronously rebuilds the NavMeshSurface when rooms are placed, built, or upgraded.

using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-55)]
public class NavMeshRebuilder : MonoBehaviour
{
    [Tooltip("The NavMeshSurface covering the guild floor.")]
    [SerializeField] private NavMeshSurface surface;

    private NavMeshData _data;
    private AsyncOperation _activeBake;
    private bool _rebuildQueued;

    private void Awake()
    {
        if (!surface)
        {
            Debug.LogError("[NavMeshRebuilder] No NavMeshSurface assigned.");
            enabled = false;
            return;
        }
        if (!surface.navMeshData)
            _activeBake = surface.UpdateNavMesh(new NavMeshData());
    }

    private void OnEnable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onRoomPlaced.AddListener(HandleGeometryChanged);
        relay.onFacilityBuilt.AddListener(HandleGeometryChanged);
        relay.onFacilityUpgraded.AddListener(HandleGeometryChanged);
    }

    private void OnDisable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onRoomPlaced.RemoveListener(HandleGeometryChanged);
        relay.onFacilityBuilt.RemoveListener(HandleGeometryChanged);
        relay.onFacilityUpgraded.RemoveListener(HandleGeometryChanged);
    }
    
    private void HandleGeometryChanged(FacilityType _)
        => _rebuildQueued = true;
    
    private void Update()
    {
        if (!_rebuildQueued)
            return;
        if (_activeBake is { isDone: false })
            return;

        _rebuildQueued = false;
        _activeBake = surface.UpdateNavMesh(surface.navMeshData);
    }
}