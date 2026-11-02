// HUD build button (left of the pause button): toggles build mode; mouse-only, never focusable.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class HudBuildButtonPresenter : MonoBehaviour
    {
        private const string ActiveClass = "hud-build--active";

        [Tooltip("For the hammer icon. Empty slot (or no asset) uses the runtime placeholder.")]
        [SerializeField] private GuildHudArt art;

        private Button _button;

        // NOTE: Never focusable — Space is both UI Submit and the time-pause hotkey.
        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _button = root?.Q<Button>("hud-build");
            var icon = root?.Q<VisualElement>("hud-build-icon");

            if (_button == null || icon == null)
            {
                Debug.LogError("[HudBuildButtonPresenter] Missing 'hud-build' or 'hud-build-icon' in the HUD UXML.", this);
                return;
            }

            _button.focusable = false;
            _button.RemoveFromClassList(Button.ussClassName);

            Sprite sprite = art ? art.Build : null;
            icon.style.backgroundImage = new StyleBackground(
                sprite ? Background.FromSprite(sprite) : Background.FromTexture2D(PlaceholderIcons.Hammer));

            _button.clicked += OnClicked;
            GameEventsRelay.OnBuildModeChanged.AddListener(OnBuildModeChanged);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.clicked -= OnClicked;
            GameEventsRelay.OnBuildModeChanged.RemoveListener(OnBuildModeChanged);
        }

        private static void OnClicked()
        {
            if (BuildModeController.HasInstance)
                BuildModeController.Instance.Toggle();
        }

        private void OnBuildModeChanged(bool active) => _button?.EnableInClassList(ActiveClass, active);
    }
}