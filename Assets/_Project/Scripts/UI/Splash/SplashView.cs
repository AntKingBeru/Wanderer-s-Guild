using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class SplashView : MonoBehaviour
    {
        private const string RootName = "splash-root";
        private const string ContentName = "splash-content";
        private const string LogoName = "splash-logo";
        private const string PlaceholderName = "splash-placeholder";

        private VisualElement _root;
        private VisualElement _content;
        private VisualElement _logo;
        private OutlinedText _placeholder;
        private float _contentOpacity;

        public bool IsBound { get; private set; }

        //Opacity of the logo or placeholder (0–1). Driven by SplashSequenceStep tweens.<
        public float ContentOpacity
        {
            get => _contentOpacity;
            set
            {
                _contentOpacity = Mathf.Clamp01(value);
                if (IsBound) _content.style.opacity = _contentOpacity;
            }
        }

        private void OnEnable()
        {
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            _root = documentRoot?.Q<VisualElement>(RootName);
            _content = documentRoot?.Q<VisualElement>(ContentName);
            _logo = documentRoot?.Q<VisualElement>(LogoName);
            _placeholder = documentRoot?.Q<OutlinedText>(PlaceholderName);

            IsBound = _root != null && _content != null && _logo != null && _placeholder != null;
            if (!IsBound)
            {
                Debug.LogError("[SplashView] Missing one or more splash elements in the UXML.", this);
                return;
            }

            if (_root != null)
                _root.style.backgroundColor = GameConfig.Instance.Splash.BackgroundColor;
            ContentOpacity = 0f;
        }

        //Shows the slide's logo, or its placeholder text when no logo is assigned.
        public void ShowSlide(SplashSlide slide)
        {
            if (!IsBound) return;

            var hasLogo = slide.Logo;
            _logo.style.display = hasLogo ? DisplayStyle.Flex : DisplayStyle.None;
            _placeholder.style.display = hasLogo ? DisplayStyle.None : DisplayStyle.Flex;

            if (hasLogo)
                _logo.style.backgroundImage = new StyleBackground(slide.Logo);
            else
                _placeholder.Text = slide.PlaceholderText;
        }
    }
}