// A room light that fades in as daylight drops (InteriorLightDirector drives it), with an optional gentle flicker.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: On a point/spotLight in a room prefab. Its authored intensity is the full brightness. Registers only while
    // enabled; disabled → back to the authored intensity (prefabs/ghosts never keep a dimmed value).
    [RequireComponent(typeof(Light))]
    [DisallowMultipleComponent]
    public class InteriorLight : MonoBehaviour
    {
        private static readonly List<InteriorLight> Active = new();

        [Tooltip("Flicker depth (0 = steady, 0.15 = candle).")]
        [SerializeField, Range(0f, 0.5f)] private float flicker;
        [Tooltip("Flicker speed.")]
        [SerializeField, Min(0.1f)] private float flickerSpeed = 6f;

        private Light _light;
        private float _fullIntensity;
        private float _seed;

        public static IReadOnlyList<InteriorLight> All => Active;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _fullIntensity = _light.intensity;
            _seed = Random.value * 100f;
        }

        // NOTE: Applies the director's last strength at once — a room finishing at night lights up immediately.
        private void OnEnable()
        {
            Active.Add(this);
            Apply(InteriorLightDirector.Strength, Time.time);
        }

        private void OnDisable()
        {
            Active.Remove(this);
            if (_light != null)
            {
                _light.intensity = _fullIntensity;
                _light.enabled = true;
            }
        }

        // Sets the light for this strength (0 off … 1 full); flicker varies it over time.
        public void Apply(float strength, float time)
        {
            var wobble = flicker > 0f ? 1f - flicker * Mathf.PerlinNoise(_seed, time * flickerSpeed) : 1f;
            var intensity = _fullIntensity * strength * wobble;

            _light.enabled = intensity > 0.001f;
            _light.intensity = intensity;
        }
    }
}