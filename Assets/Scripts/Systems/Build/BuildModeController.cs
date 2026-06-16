// Drives per-frame hover detection for all BuildDoor props in the scene.
// Shows/hides doors when build mode is toggled via Observer (BuildManager.OnBuildModeChanged).
// Keeps BuildDoor and BuildManager decoupled — neither knows about the other directly.
// Attach to a persistent manager GameObject.

using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    [Tooltip("Camera used to build hover rays. Falls back to Camera.main.")]
    [SerializeField] private Camera cam;
    
    // All doors in the scene; populated automatically in Awake.
    private BuildDoor[] _allDoors;
    
    private void Awake()
    {
        if (!cam)
            cam = Camera.main;
    }

    private void OnEnable()
        => GameEventRelay.Instance.onBuildModeChanged.AddListener(HandleBuildModeChanged);

    private void OnDisable()
        => GameEventRelay.Instance.onBuildModeChanged.RemoveListener(HandleBuildModeChanged);

    private void Update()
    {
        // Only poll hover while in build mode.
        if (!BuildManager.Instance || !BuildManager.Instance.BuildModeActive)
            return;
        if (!cam)
            return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        foreach (var door in _allDoors)
        {
            if (door && door.gameObject.activeSelf)
                door.UpdateHover(ray);
        }
    }
    
    private void HandleBuildModeChanged(bool isActive)
    {
        // Re-collect doors each time build mode is entered to pick up any dynamically spawned ones.
        if (isActive)
            _allDoors = FindObjectsByType<BuildDoor>();

        foreach (var door in _allDoors)
            if (door) door.gameObject.SetActive(isActive);
    }
}