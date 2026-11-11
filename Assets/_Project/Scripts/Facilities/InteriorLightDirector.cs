// Remembers the current daylight and drives every interior light each frame (fade + flicker).
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: On the Directional Light (with SunController). Daylight arrives once per in-game minute; lights are updated every
    // frame for the flicker. Strength is static so a light enabled later (a room finishing at night) starts correct.
    public class InteriorLightDirector : MonoBehaviour
    {
        // The current interior-light strength (0 off … 1 full).
        public static float Strength { get; private set; }

        private void OnEnable() => GameEventsRelay.OnDaylightChanged.AddListener(OnDaylightChanged);
        private void OnDisable() => GameEventsRelay.OnDaylightChanged.RemoveListener(OnDaylightChanged);

        private void OnDaylightChanged(float daylight) => Strength = GameConfig.Instance.InteriorLights.Strength(daylight);

        private void Update()
        {
            var lights = InteriorLight.All;
            float strength = Strength, time = Time.time;
            for (var i = lights.Count - 1; i >= 0; i--)
                lights[i].Apply(strength, time);
        }

        // NOTE: With domain reload disabled, a stale strength from the last Play session would light rooms at noon.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Strength = 0f;
    }
}