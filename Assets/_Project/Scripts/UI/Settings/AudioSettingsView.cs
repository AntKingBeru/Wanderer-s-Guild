using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class AudioSettingsView
    {
        private readonly Dictionary<AudioChannel, SliderInt> _sliders = new();
        private readonly Dictionary<AudioChannel, OutlinedText> _values = new();
        private readonly List<SliderInt> _ordered = new();
        private int _step = 5;

        // The sliders in on-screen order. Used for the focus order and slider stepping.
        public IReadOnlyList<SliderInt> Sliders => _ordered;

        private AudioSettingsView() { }

        // Finds each channel's slider ("volume-x") and readout ("volume-x-value"). Returns null if any is missing.
        public static AudioSettingsView Bind(VisualElement root)
        {
            AudioSettingsView view = new();
            foreach (var channel in (AudioChannel[])Enum.GetValues(typeof(AudioChannel)))
            {
                var key = Key(channel);
                var slider = root.Q<SliderInt>($"volume-{key}");
                var value = root.Q<OutlinedText>($"volume-{key}-value");
                if (slider == null || value == null) return null;

                view._sliders[channel] = slider;
                view._values[channel] = value;
                view._ordered.Add(slider);
            }
            return view;
        }

        public void Initialize(int step)
        {
            _step = Mathf.Max(1, step);

            foreach (var (channel, slider) in _sliders)
            {
                slider.lowValue = 0;
                slider.highValue = 100;
                slider.RegisterValueChangedCallback(evt => OnChanged(channel, slider, evt.newValue));
            }
        }

        // Pulls the current values from SettingsManager without triggering change callbacks.
        public void Refresh()
        {
            if (!SettingsManager.HasInstance)
                return;

            foreach (var pair in _sliders)
            {
                var value = SettingsManager.Instance.GetVolume(pair.Key);
                pair.Value.SetValueWithoutNotify(value);
                _values[pair.Key].Text = value.ToString();
            }
        }

        // NOTE: Mouse drags produce every integer; snapping keeps values on the step grid the player sees.
        private void OnChanged(AudioChannel channel, SliderInt slider, int value)
        {
            var snapped = Mathf.Clamp(Mathf.RoundToInt(value / (float)_step) * _step, 0, 100);
            if (snapped != value)
                slider.SetValueWithoutNotify(snapped);

            _values[channel].Text = snapped.ToString();

            if (SettingsManager.HasInstance)
                SettingsManager.Instance.SetVolume(channel, snapped);
        }

        private static string Key(AudioChannel channel) => channel switch
        {
            AudioChannel.Master => "master",
            AudioChannel.Music => "music",
            AudioChannel.Sfx => "sfx",
            AudioChannel.Character => "character",
            _ => string.Empty
        };
    }
}