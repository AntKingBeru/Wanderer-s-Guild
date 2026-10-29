using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class AudioSettingsApplier
    {
        // NOTE: Unity's mixer floor; treated as silence.
        private const float MinDecibels = -80f;

        private readonly AudioMixerSettings _settings;
        private readonly HashSet<string> _warnedParameters = new();
        private bool _warnedMissingMixer;

        public AudioSettingsApplier(AudioMixerSettings settings) => _settings = settings;

        public void ApplyAll(AudioSettingsData audio)
        {
            Apply(AudioChannel.Master, audio.Get(AudioChannel.Master));
            Apply(AudioChannel.Music, audio.Get(AudioChannel.Music));
            Apply(AudioChannel.Sfx, audio.Get(AudioChannel.Sfx));
            Apply(AudioChannel.Character, audio.Get(AudioChannel.Character));
        }

        // NOTE: Must not run from Awake. AudioMixer.SetFloat is ignored there, so the first call comes from a boot step.
        public void Apply(AudioChannel channel, int percent)
        {
            if (!_settings.Mixer)
            {
                if (!_warnedMissingMixer)
                    Debug.LogWarning("[AudioSettingsApplier] No AudioMixer assigned in GameConfig › Audio Mixer.");
                _warnedMissingMixer = true;
                return;
            }

            var parameter = _settings.ParameterFor(channel);
            if (!_settings.Mixer.SetFloat(parameter, ToDecibels(percent)) && _warnedParameters.Add(parameter))
                Debug.LogWarning($"[AudioSettingsApplier] Mixer has no exposed parameter '{parameter}'. Check the name.");
        }

        // 0 → −80 dB (silent); otherwise 20·log10(percent / 100), so 100 → 0 dB and 50 → about −6 dB.
        public static float ToDecibels(int percent) =>
            percent <= 0 ? MinDecibels : 20f * Mathf.Log10(Mathf.Clamp(percent, 0, 100) / 100f);
    }
}