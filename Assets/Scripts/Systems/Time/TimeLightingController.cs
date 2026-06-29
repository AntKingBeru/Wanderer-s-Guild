// Applies the active DayLightingProfile to the scene's sun + ambient, driven by TimeController.

using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(20)]
public class TimeLightingController : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Directional light acting as the sun.")]
    [SerializeField] private Light sun;
    
    [Header("Seasonal Profiles — order MUST match: Spring, Summer, Autumn, Winter")]
    [SerializeField] private DayLightingProfile[] profiles = new DayLightingProfile[4];
    
    [Header("Seasonal Transition")]
    [Tooltip("Real seconds to cross-fade from the old season's lighting to the new one.")]
    [SerializeField] private float seasonFadeSeconds = 3f;

    [Header("Optional Smoothing")]
    [Tooltip("0 = snap to the sampled value. Higher = softer easing (helps at Very Fast).")]
    [SerializeField] private float smoothing;
    
    private DayLightingProfile _fromProfile;
    private DayLightingProfile _toProfile;
    private float _fadeElapsed;
    private bool _initialized;
    
    private Color _currentSunColor;
    private Color _currentAmbient;
    private float _currentSunIntensity;

    private void Awake()
        => RenderSettings.ambientMode = AmbientMode.Flat;
    
    private void OnEnable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onSeasonChanged.AddListener(HandleSeasonChanged);
        SyncToCurrentSeason();
    }
    
    private void OnDisable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onSeasonChanged.RemoveListener(HandleSeasonChanged);
    }
    
    private void SyncToCurrentSeason()
    {
        if (!TimeController.Exists)
            return;
        _toProfile = ProfileFor(TimeController.Instance.CurrentDate.season);
        _fromProfile = _toProfile;
        _fadeElapsed = seasonFadeSeconds;
        _initialized = true;
    }
    
    private void HandleSeasonChanged(Season season)
    {
        var next = ProfileFor(season);
        if (next == null)
            return;
        _fromProfile = _toProfile ? _toProfile : next;
        _toProfile = next;
        _fadeElapsed = 0f;
    }
    
    private DayLightingProfile ProfileFor(Season season)
    {
        var i = (int)season;
        return i >= 0 && i < profiles.Length ? profiles[i] : null;
    }

    private void LateUpdate()
    {
        if (!sun || !TimeController.Exists)
            return;
        if (!_initialized)
            SyncToCurrentSeason();
        if (!_toProfile)
            return;
        
        var tod = TimeController.Instance.TimeOfDay;
        var running = TimeController.Instance.CurrentSpeed != TimeSpeed.Pause;
        
        if (running && _fadeElapsed < seasonFadeSeconds)
            _fadeElapsed += Time.deltaTime;
        
        var fade = seasonFadeSeconds <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / seasonFadeSeconds);
        
        var target = !_fromProfile || _fromProfile == _toProfile || fade >= 1f
            ? _toProfile.Evaluate(tod)
            : LightingSample.Lerp(_fromProfile.Evaluate(tod), _toProfile.Evaluate(tod), fade);

        sun.transform.rotation = target.sunRotation;
        
        if (smoothing <= 0f || !running)
        {
            _currentSunColor = target.sunColor;
            _currentAmbient = target.ambientColor;
            _currentSunIntensity = target.sunIntensity;
        }
        else
        {
            var k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);   // framerate-independent easing
            _currentSunColor = Color.Lerp(_currentSunColor, target.sunColor, k);
            _currentAmbient = Color.Lerp(_currentAmbient, target.ambientColor, k);
            _currentSunIntensity = Mathf.Lerp(_currentSunIntensity, target.sunIntensity, k);
        }

        sun.color = _currentSunColor;
        RenderSettings.ambientLight = _currentAmbient;
        sun.intensity = _currentSunIntensity;
    }
}