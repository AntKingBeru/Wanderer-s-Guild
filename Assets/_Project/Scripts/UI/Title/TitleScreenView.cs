using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class TitleScreenView : MonoBehaviour
    {
        private const string PromptName = "title-prompt";

        private VisualElement _prompt;
        private Tween _promptTween;
        private float _promptOpacity = 1f; // NOTE: Cached source of truth (resolvedStyle lags behind inline writes).

        private void OnEnable()
        {
            // NOTE: Queried as VisualElement, so it works whether the prompt is an OutlinedText or a plain Label.
            _prompt = GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>(PromptName);
            if (_prompt == null)
            {
                Debug.LogError($"[TitleScreenView] Element '{PromptName}' not found in the UIDocument.", this);
                return;
            }

            ApplyPromptOpacity(_promptOpacity);
        }

        public void SetPromptOpacityImmediate(float opacity)
        {
            KillTween();
            ApplyPromptOpacity(opacity);
        }

        //Starts fading the prompt out. Fire-and-forget; it runs alongside the camera pan.
        public void HidePrompt()
        {
            KillTween();
            _promptTween = DOVirtual.Float(_promptOpacity, 0f, GameConfig.Instance.TitleScreen.PromptFadeOutDuration, ApplyPromptOpacity)
                .SetEase(Ease.OutSine)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillTween()
        {
            if (_promptTween.IsActive())
                _promptTween.Kill();
        }

        private void ApplyPromptOpacity(float value)
        {
            _promptOpacity = Mathf.Clamp01(value);
            if (_prompt != null)
                _prompt.style.opacity = _promptOpacity;
        }
    }
}