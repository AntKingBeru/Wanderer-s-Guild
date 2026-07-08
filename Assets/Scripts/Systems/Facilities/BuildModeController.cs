// Singleton State controller: toggles build mode, highlights doors, and opens the radial on door click.

using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-58)]
public class BuildModeController : MonoSingleton<BuildModeController>
{
    [Header("Input")]
    [SerializeField] private InputActionReference toggleBuild;
    [SerializeField] private InputActionReference click;
    [SerializeField] private InputActionReference pointer;

    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask doorLayers = ~0;

    [Header("Content")]
    [SerializeField] private GameObject doorMarkerPrefab;

    public bool IsActive { get; private set; }

    private DoorHighlighter _doorHighlighter;

    protected override void OnSingletonAwake() => _doorHighlighter = new DoorHighlighter(doorMarkerPrefab);

    private void OnEnable()
    {
        if (toggleBuild?.action != null)
        {
            toggleBuild.action.performed += OnToggle;
            toggleBuild.action.Enable();
        }
        if (click?.action != null)
        { click.action.performed += OnClick;
            click.action.Enable(); }
        pointer?.action?.Enable();
    }
    
    private void OnDisable()
    {
        if (toggleBuild?.action != null)
            toggleBuild.action.performed -= OnToggle;
        if (click?.action != null)
            click.action.performed -= OnClick;
    }

    private void OnToggle(InputAction.CallbackContext _) => SetActive(!IsActive);

    public void SetActive(bool active)
    {
        if (active == IsActive)
            return;
        IsActive = active;

        if (active)
            _doorHighlighter.Show();
        else
        {
            _doorHighlighter.Hide();
            if (RadialMenuController.Exists)
                RadialMenuController.Instance.Close();
        }

        GameEventsRelay.Instance.RaiseBuildModeChanged(active);
    }

    // Door click (only while active, and only when the radial isn't already open handling clicks).
    private void OnClick(InputAction.CallbackContext _)
    {
        if (!IsActive)
            return;
        if (RadialMenuController.Exists && RadialMenuController.Instance.IsOpen)
            return;
        if (!cam)
            cam = Camera.main;
        if (!cam || pointer?.action == null)
            return;

        var p = pointer.action.ReadValue<Vector2>();
        var ray = cam.ScreenPointToRay(p);
        if (Physics.Raycast(ray, out var hit, 200f, doorLayers, QueryTriggerInteraction.Collide))
        {
            var marker = hit.collider.GetComponentInParent<DoorMarker>();
            if (marker && RadialMenuController.Exists)
                RadialMenuController.Instance.Open(marker.Door, marker.WorldPosition);
        }
    }
    
    private void Start()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onRoomPlaced.AddListener(_ => { if (IsActive) _doorHighlighter.Show(); });
    }
}