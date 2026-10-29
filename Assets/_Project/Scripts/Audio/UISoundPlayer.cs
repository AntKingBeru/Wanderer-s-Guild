using UnityEngine;

namespace WanderersGuild
{
    public class UISoundPlayer : Singleton<UISoundPlayer>
    {
        private AudioSource _source;
        private float _lastHoverTime = float.NegativeInfinity;

        protected override void OnSingletonAwake()
        {
            var settings = GameConfig.Instance.UISounds;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
            _source.outputAudioMixerGroup = settings.OutputGroup;

            GameEventsRelay.OnUIButtonFocused.AddListener(OnButtonFocused);
            GameEventsRelay.OnUIButtonClicked.AddListener(OnButtonClicked);
            GameEventsRelay.OnMenuPanelOpened.AddListener(OnPanelOpened);
            GameEventsRelay.OnMenuPanelClosed.AddListener(OnPanelClosed);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnUIButtonFocused.RemoveListener(OnButtonFocused);
            GameEventsRelay.OnUIButtonClicked.RemoveListener(OnButtonClicked);
            GameEventsRelay.OnMenuPanelOpened.RemoveListener(OnPanelOpened);
            GameEventsRelay.OnMenuPanelClosed.RemoveListener(OnPanelClosed);
            base.OnDestroy();
        }

        // NOTE: The cooldown stops a fast mouse sweep across the menu from stacking a dozen hover sounds.
        private void OnButtonFocused()
        {
            var settings = GameConfig.Instance.UISounds;
            if (Time.unscaledTime - _lastHoverTime < settings.HoverCooldown)
                return;

            _lastHoverTime = Time.unscaledTime;
            Play(settings.Hover);
        }

        private void OnButtonClicked() => Play(GameConfig.Instance.UISounds.Click);
        private void OnPanelOpened(MenuPanelId _) => Play(GameConfig.Instance.UISounds.PanelOpen);
        private void OnPanelClosed(MenuPanelId _) => Play(GameConfig.Instance.UISounds.PanelClose);

        private void Play(UISoundClip sound)
        {
            if (!sound.Clip || !_source)
                return;
            _source.PlayOneShot(sound.Clip, sound.Volume);
        }
    }
}