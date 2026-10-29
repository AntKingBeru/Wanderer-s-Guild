// Binds the save toast (bottom-right): shows a message, optionally as an error, fading in and out on unscaled time.
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class SaveToastView : MonoBehaviour
    {
        private const string RootName = "toast-root";
        private const string LabelName = "toast-label";
        private const string ErrorClass = "toast--error";

        private VisualElement _root;
        private OutlinedText _label;
        private Sequence _sequence;
        private float _opacity; // NOTE: Cached source of truth (resolvedStyle lags behind inline writes).

        public bool IsBound { get; private set; }

        private void OnEnable()
        {
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            _root = documentRoot?.Q<VisualElement>(RootName);
            _label = documentRoot?.Q<OutlinedText>(LabelName);

            IsBound = _root != null && _label != null;
            if (!IsBound)
            {
                Debug.LogError("[SaveToastView] Missing toast-root or toast-label in the UXML.", this);
                return;
            }

            SetOpacity(0f);
        }

        /// <param name="message"></param>
        /// <param name="isError"></param>
        /// <param name="holdSeconds">How long to stay fully visible before fading out; negative = stay until the next Show.</param>
        public void Show(string message, bool isError, float holdSeconds)
        {
            if (!IsBound)
                return;

            Kill();
            _label.Text = message;
            _root.EnableInClassList(ErrorClass, isError);

            // NOTE: One sequence (fade in → hold → fade out), so a quick "Saving..." → "Saved" never leaves it half-faded.
            var fade = GameConfig.Instance.SaveGame.ToastFadeDuration;
            _sequence = DOTween.Sequence()
                .Append(DOVirtual.Float(_opacity, 1f, fade, SetOpacity))
                .SetUpdate(true)
                .SetLink(gameObject);

            if (holdSeconds >= 0f)
            {
                _sequence.AppendInterval(holdSeconds);
                _sequence.Append(DOVirtual.Float(1f, 0f, fade, SetOpacity));
            }
        }

        private void SetOpacity(float value)
        {
            _opacity = Mathf.Clamp01(value);
            if (IsBound) _root.style.opacity = _opacity;
        }

        private void Kill()
        {
            if (_sequence.IsActive())
                _sequence.Kill();
        }
    }
}