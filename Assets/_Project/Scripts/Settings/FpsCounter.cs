using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class FpsCounter : Singleton<FpsCounter>
    {
        private const string RootName = "fps-root";
        private const string LabelName = "fps-label";

        private VisualElement _root;
        private OutlinedText _label;
        private bool _visible;
        private int _frames;
        private float _elapsed;

        private void OnEnable()
        {
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            _root = documentRoot?.Q<VisualElement>(RootName);
            _label = documentRoot?.Q<OutlinedText>(LabelName);

            if (_root == null || _label == null)
                Debug.LogError("[FpsCounter] Missing fps-root or fps-label in the UXML.", this);

            ApplyVisibility();
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            _frames = 0;
            _elapsed = 0f;
            ApplyVisibility();
        }

        // NOTE: Unscaled time, so the readout stays correct while the game is paused.
        private void Update()
        {
            if (!_visible || _label == null)
                return;

            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < GameConfig.Instance.DisplaySettings.FpsCounterInterval)
                return;

            _label.Text = $"{Mathf.RoundToInt(_frames / _elapsed)} FPS";
            _frames = 0;
            _elapsed = 0f;
        }

        private void ApplyVisibility()
        {
            if (_root != null)
                _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}