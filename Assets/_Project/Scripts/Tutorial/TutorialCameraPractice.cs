// The controls intro's "try it": done once the player has both moved and zoomed the camera since it began.
namespace WanderersGuild
{
    // NOTE: Listens only while practising; stops by itself when done, and on End (the next step, or the tutorial stopping).
    public sealed class TutorialCameraPractice
    {
        private bool _panned;
        private bool _zoomed;
        private bool _listening;

        public bool IsDone => _panned && _zoomed;

        public void Begin()
        {
            End();
            _panned = false;
            _zoomed = false;
            GameEventsRelay.OnCameraPannedByPlayer.AddListener(OnPanned);
            GameEventsRelay.OnCameraZoomedByPlayer.AddListener(OnZoomed);
            _listening = true;
        }

        public void End()
        {
            if (!_listening)
                return;

            GameEventsRelay.OnCameraPannedByPlayer.RemoveListener(OnPanned);
            GameEventsRelay.OnCameraZoomedByPlayer.RemoveListener(OnZoomed);
            _listening = false;
        }

        private void OnPanned()
        {
            _panned = true;
            if (IsDone)
                End();
        }

        private void OnZoomed()
        {
            _zoomed = true;
            if (IsDone)
                End();
        }
    }
}