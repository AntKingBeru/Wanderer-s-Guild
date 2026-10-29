using System;
using UnityEngine;

namespace WanderersGuild
{
    [Serializable]
    public class AudioSettingsData
    {
        [SerializeField] private int master = 100;
        [SerializeField] private int music = 100;
        [SerializeField] private int sfx = 100;
        [SerializeField] private int character = 100;

        public int Get(AudioChannel channel) => channel switch
        {
            AudioChannel.Master => master,
            AudioChannel.Music => music,
            AudioChannel.Sfx => sfx,
            AudioChannel.Character => character,
            _ => 100
        };

        public void Set(AudioChannel channel, int percent)
        {
            var value = Mathf.Clamp(percent, 0, 100);
            switch (channel)
            {
                case AudioChannel.Master:
                    master = value;
                    break;
                case AudioChannel.Music:
                    music = value;
                    break;
                case AudioChannel.Sfx:
                    sfx = value;
                    break;
                case AudioChannel.Character:
                    character = value;
                    break;
            }
        }

        // Clamps every value into 0–100, in case the file was hand-edited or corrupted.
        public void Sanitize()
        {
            master = Mathf.Clamp(master, 0, 100);
            music = Mathf.Clamp(music, 0, 100);
            sfx = Mathf.Clamp(sfx, 0, 100);
            character = Mathf.Clamp(character, 0, 100);
        }
    }
}