// ScriptableObject lighting profile: maps a 0..1 day fraction to sun + ambient values (pure).

using UnityEngine;

[CreateAssetMenu(fileName = "DayLightingProfile", menuName = "Wanderer's Guild/Day Lighting Profile")]
public class DayLightingProfile : ScriptableObject
{
    [Header("Sun Orientation")]
    [Tooltip("Compass direction (Y rotation) the sun travels along. Pitch is derived from time.")]
    [SerializeField] private float sunYaw = 170f;
    
    [Header("Over-Day Curves (key = 0 midnight → 0.5 midday → 1 midnight)")]
    [SerializeField] private Gradient sunColorOverDay;
    [SerializeField] private AnimationCurve sunIntensityOverDay;
    [SerializeField] private Gradient ambientColorOverDay;
    
    public LightingSample Evaluate(float t)
    {
        t = Mathf.Repeat(t, 1f);   // safety: wrap any out-of-range input

        // Pitch: -90° (below, midnight) → 0° (horizon, ~06:00) → 90° (overhead, midday) → 270° (wraps).
        var rotation = Quaternion.Euler(t * 360f - 90f, sunYaw, 0f);

        return new LightingSample
        (
            rotation,
            sunColorOverDay.Evaluate(t),
            ambientColorOverDay.Evaluate(t),
            sunIntensityOverDay.Evaluate(t)
        );
    }
    
    private void Reset()
    {
        sunColorOverDay = BuildGradient
        (
            (0.00f, new Color(0.10f, 0.12f, 0.25f)), // midnight — cold blue
            (0.25f, new Color(0.95f, 0.55f, 0.35f)), // dawn — warm orange
            (0.50f, new Color(1.00f, 0.96f, 0.88f)), // midday — near white
            (0.75f, new Color(0.95f, 0.50f, 0.30f)), // dusk — orange
            (1.00f, new Color(0.10f, 0.12f, 0.25f))  // midnight again
        );

        ambientColorOverDay = BuildGradient
        (
            (0.00f, new Color(0.05f, 0.06f, 0.12f)),
            (0.50f, new Color(0.55f, 0.58f, 0.62f)),
            (1.00f, new Color(0.05f, 0.06f, 0.12f))
        );

        sunIntensityOverDay = BuildCurve
        (
            (0.00f, 0.02f), (0.22f, 0.05f), (0.27f, 0.55f),
            (0.50f, 1.10f),
            (0.73f, 0.55f), (0.78f, 0.05f), (1.00f, 0.02f)
        );
    }
    
    private static Gradient BuildGradient(params (float time, Color color)[] stops)
    {
        var colorKeys = new GradientColorKey[stops.Length];
        for (var i = 0; i < stops.Length; i++)
            colorKeys[i] = new GradientColorKey(stops[i].color, stops[i].time);

        var gradient = new Gradient();
        gradient.SetKeys(colorKeys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }
    
    private static AnimationCurve BuildCurve(params (float time, float value)[] keys)
    {
        var frames = new Keyframe[keys.Length];
        for (var i = 0; i < keys.Length; i++)
            frames[i] = new Keyframe(keys[i].time, keys[i].value);

        var curve = new AnimationCurve(frames);
        for (var i = 0; i < curve.length; i++)
            curve.SmoothTangents(i, 0f);
        return curve;
    }
}