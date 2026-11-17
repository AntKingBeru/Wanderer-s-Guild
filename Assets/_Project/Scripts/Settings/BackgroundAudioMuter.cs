// Silences all audio while the game is unfocused, unless the player chose to keep it playing (Options › Audio).
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: AudioListener.volume, not .pause — the music's sources ignore listener pause (menus never stop the music).
    public sealed class BackgroundAudioMuter
    {
        private bool _hasFocus = true;
        private bool _playInBackground = true;

        public void SetPlayInBackground(bool play)
        {
            _playInBackground = play;
            Apply();
        }

        public void OnFocusChanged(bool hasFocus)
        {
            _hasFocus = hasFocus;
            Apply();
        }

        private void Apply() => AudioListener.volume = _hasFocus || _playInBackground ? 1f : 0f;
    }
}