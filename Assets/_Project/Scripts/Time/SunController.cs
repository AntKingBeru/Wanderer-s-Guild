// The guild's sun: drives the directional light and the ambient light from the in-game clock (once per in-game minute).
using UnityEngine;
using UnityEngine.Rendering;

namespace WanderersGuild
{
    // NOTE: Play mode only — in the editor the scene keeps its authored light. Put it on the scene's Directional Light.
    [RequireComponent(typeof(Light))]
    public class SunController : MonoBehaviour
    {
        private Light _light;
        private LightingSettings _settings;
        private long _lastMinute = long.MinValue;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _settings = GameConfig.Instance.Lighting;

            _light.type = LightType.Directional;
            _light.shadows = _settings.Shadows;
            RenderSettings.ambientMode = AmbientMode.Flat;
        }

        // NOTE: The clock only changes per in-game minute; re-evaluating only then keeps it cheap (and smooth at Very Fast).
        private void LateUpdate()
        {
            var now = RequestManager.NowMinutes;
            if (now == _lastMinute)
                return;
            _lastMinute = now;

            var dayFraction = (float)(now % GameCalendar.MinutesPerDay) / GameCalendar.MinutesPerDay;
            var sun = DayNightCycle.Evaluate(dayFraction, _settings);

            transform.rotation = sun.Rotation;
            _light.color = sun.Color;
            _light.intensity = sun.Intensity;
            RenderSettings.ambientLight = sun.Ambient;

            GameEventsRelay.OnDaylightChanged.Invoke(sun.Daylight);
        }
    }
}