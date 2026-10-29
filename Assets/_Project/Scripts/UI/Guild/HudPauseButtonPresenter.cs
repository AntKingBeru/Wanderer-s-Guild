// HUD pause button (right of the gold): opens the pause menu exactly like Esc; mouse-only, never focusable.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class HudPauseButtonPresenter : MonoBehaviour
    {
        [SerializeField] private PauseMenuController pauseMenu;

        [Tooltip("For the pause icon. Empty slot (or no asset) uses the runtime placeholder.")]
        [SerializeField] private GuildHudArt art;

        private Button _button;

        // NOTE: Never focusable — Space is both UI Submit and the time-pause hotkey, and would trigger both.
        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _button = root?.Q<Button>("hud-pause");
            var icon = root?.Q<VisualElement>("hud-pause-icon");

            if (_button == null || icon == null)
            {
                Debug.LogError("[HudPauseButtonPresenter] Missing 'hud-pause' or 'hud-pause-icon' in the HUD UXML.", this);
                return;
            }

            _button.focusable = false;
            _button.RemoveFromClassList(Button.ussClassName);

            var sprite = art ? art.Pause : null;
            icon.style.backgroundImage = new StyleBackground(
                sprite ? Background.FromSprite(sprite) : Background.FromTexture2D(PlaceholderIcons.Pause));

            _button.clicked += OnClicked;
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.clicked -= OnClicked;
        }

        private void OnClicked()
        {
            if (pauseMenu)
                pauseMenu.TryOpen();
        }
    }
}