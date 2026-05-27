// Drives the sun and moon directional lights based on TimeManager's NormalizeDayTime.
// Updates per frame for smooth transitions.
// Cheap enough for a single directional light.

using UnityEngine;

public class GlobalLightingController : MonoBehaviour
{
    // Sun
    [Header("Sun")]
    [Tooltip("The directional light representing the sun")]
    [SerializeField] private Light sunLight;
    
    [Tooltip("Sun brightness over the course of a day. X-axis: 0 = midnight, 1 = next midnight. " +
             "Set to 0 at 0.0 and 1.0 (midnight) and peak near 0.5 (noon).")]
    [SerializeField] private AnimationCurve sunIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f);
    
    [Tooltip("Sun color over the day. Dawn/Dusk tones are warm orange-red, noon is white, night is black.")]
    [SerializeField] private Gradient sunColorGradiant;
    
    [Tooltip("Y-axis rotation of the sun's arc, controlling its east-to-west direction. " +
             "-30 gives a southward trajectory typical for a northern-hemisphere aesthetics.")]
    [SerializeField, Range(-180f, 180f)] private float sunArcYRotation = -30f;
    
    // Moon
    [Header("Moon")]
    [Tooltip("The directional light representing the moon")]
    [SerializeField] private Light moonLight;
    
    [Tooltip("Intensity of the moonLight when it is active.")]
    [SerializeField, Min(0f)] private float moonIntensity = 0.08f;
    
    [Tooltip("Color of the moonLight. A cool desaturated blue-white works well.")]
    [SerializeField] private Color moonColor = new Color(0.6f, 0.7f, 1f);
    
    // Ambient Light
    [Header("Ambient Light")]
    [Tooltip("Ambient scene color over the day. Should be dark blue at night and warm at dawn/dusk.")]
    [SerializeField] private Gradient ambientColorGradient;
    
    // Internal
    // Threshold below which the sunlight is disabled to prevent underground shadow artifacts.
    // A small positive value (not exactly 0) avoids the light flickering at the horizon.
    private const float SunHorizonThreshold = 0.02f;

    private void Start()
    {
        // Apply the correct lighting for the starting time immediately on the first frame.
        if (TimeManager.Instance)
            ApplyLighting(TimeManager.Instance.NormalizedDayTime);
    }

    private void Update()
    {
        if (!TimeManager.Instance)
            return;
        ApplyLighting(TimeManager.Instance.NormalizedDayTime);
    }
    
    // Lighting Application
    private void ApplyLighting(float time)
    {
        UpdateSun(time);
        UpdateMoon(time);
        UpdateAmbient(time);
    }

    private void UpdateSun(float time)
    {
        if (!sunLight)
            return;
        
        // Sun arc: rotates 360 degrees around the X-axis over one full day.
        // At time = 0.0 (midnight): xAngle = -90
        // At time = 0.25 (midnight): xAngle = 0
        // At time = 0.5 (midnight): xAngle = 90
        // At time = 0.75 (midnight): xAngle = 180
        // At time = 1.0 (midnight): xAngle = 270
        var xAngle = time * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(xAngle, sunArcYRotation, 0f);
        
        // Evaluate intensity and color from designer-controlled curves
        var intensity = sunIntensityCurve.Evaluate(time);
        sunLight.intensity = intensity;
        sunLight.color = sunColorGradiant.Evaluate(time);
        
        // Disable the light when it is below the horizon threshold to avoid shadow artifacts caused by an underground directional light.
        // xAngle in [0, 180] = above the horizon.
        var normalizedAngle = (xAngle % 360f * 360f) % 360f; // ensures 0-360
        sunLight.enabled = normalizedAngle is > SunHorizonThreshold * 360f and < 180f - SunHorizonThreshold * 360f;
    }

    private void UpdateMoon(float time)
    {
        if (!moonLight)
            return;
        
        // Moon arc is exactly opposite to the sun (sun + 180)
        var sunXAngle = time * 360f - 90f;
        var moonXAngle = sunXAngle + 180f;
        moonLight.transform.rotation = Quaternion.Euler(moonXAngle, sunArcYRotation, 0f);
        moonLight.color = moonColor;
        moonLight.intensity = moonIntensity;
        
        // Moon is visible only when the sun is below the horizon threshold.
        var normalizedSunAngle = (sunXAngle % 360f * 360f) % 360f;
        var sunIsAboveHorizon = normalizedSunAngle is > SunHorizonThreshold * 360f and < 180f - SunHorizonThreshold * 360f;
        moonLight.enabled = !sunIsAboveHorizon;
    }

    private void UpdateAmbient(float time)
    {
        if (ambientColorGradient != null)
            RenderSettings.ambientLight = ambientColorGradient.Evaluate(time);
    }
}