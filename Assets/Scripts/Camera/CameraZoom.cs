// Manages 5-stage camera zoom by scaling the child camera's offset distance; exposes a pan-speed multiplier.

using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [SerializeField] private InputActionReference zoom;
    
    public float PanSpeedMultiplier { get; private set; } = 1f;
    
    private int _stage;
    private Vector3 _dir;
    private float _targetDistance;
    private float _currentDistance;
    private bool _scrollActive;
    
    private void Awake()
    {
        var config = GameConfig.Instance.Camera;
        if (!cam || cam.localPosition == Vector3.zero)
        {
            Debug.LogError("[CameraZoom] Camera child must have a non-zero local offset.");
            enabled = false;
            return;
        }

        _dir = cam.localPosition.normalized;
        _stage = Mathf.Clamp(config.defaultZoomStage, 1, config.zoomStageCount);
        _currentDistance = _targetDistance = DistanceForStage(_stage);
        ApplyDistance(_currentDistance);
        UpdateMultiplier();
    }

    private void OnEnable()
    {
        zoom?.action?.Enable();
    }

    private void OnDisable()
    {
        zoom?.action?.Disable();
    }
    
    private void Update()
    {
        var scroll = zoom?.action?.ReadValue<float>() ?? 0f;
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (!_scrollActive)
            {
                _scrollActive = true;
                SetStage(scroll > 0f ? _stage - 1 : _stage + 1);
            }
        }
        else _scrollActive = false;

        var k = 1f - Mathf.Exp(-GameConfig.Instance.Camera.zoomSmoothing * Time.deltaTime);
        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, k);
        ApplyDistance(_currentDistance);
    }

    private void SetStage(int stage)
    {
        var config = GameConfig.Instance.Camera;
        stage = Mathf.Clamp(stage, 1, config.zoomStageCount);
        if (stage == _stage)
            return;
        _stage = stage;
        _targetDistance = DistanceForStage(stage);
        UpdateMultiplier();
    }

    private float DistanceForStage(int stage)
    {
        var config = GameConfig.Instance.Camera;
        var t = config.zoomStageCount <= 1 ? 0f : (stage - 1f) / (config.zoomStageCount - 1f);
        return Mathf.Lerp(config.minZoomDistance, config.maxZoomDistance, t);
    }

    private void ApplyDistance(float d)
        => cam.localPosition = _dir * d;
    
    private void UpdateMultiplier()
    {
        var config = GameConfig.Instance.Camera;
        var baseD = DistanceForStage(Mathf.Clamp(config.defaultZoomStage, 1, config.zoomStageCount));
        PanSpeedMultiplier = baseD <= 0.001f ? 1f : _targetDistance / baseD;
    }
}