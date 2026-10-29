using System;
using UnityEngine;

namespace WanderersGuild
{
    [Serializable]
    public class GameSettingsData
    {
        // NOTE: v2 added the display block. JsonUtility ignores missing fields, so older files still load.
        public const int CurrentVersion = 2;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private AudioSettingsData audio = new();
        [SerializeField] private DisplaySettingsData display = new();

        public int Version => version;

        public AudioSettingsData Audio
        {
            get => audio ??= new AudioSettingsData();
            set => audio = value ?? new AudioSettingsData();
        }

        public DisplaySettingsData Display
        {
            get => display ??= new DisplaySettingsData();
            set => display = value ?? new DisplaySettingsData();
        }

        // Repairs nulls and out-of-range values after loading, and migrates older files.
        public void Sanitize(DisplaySettingsConfig displayConfig)
        {
            Audio.Sanitize();

            // NOTE: Version-1 files have no display block; JsonUtility would leave raw field defaults, so use the configured ones.
            if (version < 2)
                display = DisplaySettingsData.CreateDefault(displayConfig);

            Display.Sanitize(displayConfig);
            version = CurrentVersion;
        }
    }
}