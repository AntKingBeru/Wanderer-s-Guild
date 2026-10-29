using System;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class QuitGameCommand : IMenuCommand
    {
        private static bool _isQuitting;

        public bool IsAvailable => true;
        public bool CanExecute => !_isQuitting;

        public void Execute()
        {
            if (_isQuitting) return;

            _isQuitting = true;
            FadeAndQuit();
        }

        // NOTE: The save-on-exit rule applies once a game is loaded; from the main menu there is nothing to save.
        private static async void FadeAndQuit()
        {
            try
            {
                if (MusicPlayer.HasInstance)
                    MusicPlayer.Instance.FadeOut();

                if (ScreenFader.HasInstance)
                    await ScreenFader.Instance.FadeOutAsync(ScreenFader.Instance.destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Fader destroyed; quit anyway.
            }
            finally
            {
                QuitNow();
            }
        }

        private static void QuitNow()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // NOTE: With domain reload disabled, the static flag would otherwise block quitting in the next Play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => _isQuitting = false;
    }
}