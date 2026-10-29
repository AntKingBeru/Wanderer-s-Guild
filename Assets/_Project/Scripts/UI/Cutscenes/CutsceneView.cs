// Binds the cutscene UXML: two-slide image layers, the scrolling narration box, the continue arrow and the skip hint.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class CutsceneView : MonoBehaviour
    {
        private const string RootName = "cutscene-root";
        private const string ImageAName = "cutscene-image-a";
        private const string ImageBName = "cutscene-image-b";
        private const string TextScrollName = "cutscene-text-scroll";
        private const string TextClipName = "cutscene-text-clip";
        private const string TextName = "cutscene-text";
        private const string PromptName = "cutscene-prompt";
        private const string SkipName = "cutscene-skip";
        private const string SkipLabelName = "cutscene-skip-label";
        private const string SkipFillName = "cutscene-skip-fill";

        private VisualElement _skip;
        private OutlinedText _skipLabel;
        private VisualElement _skipFill;

        public bool IsBound { get; private set; }
        public VisualElement Root { get; private set; }
        public VisualElement ImageA { get; private set; }
        public VisualElement ImageB { get; private set; }
        public ScrollView TextScroll { get; private set; }
        public VisualElement TextClip { get; private set; }
        public OutlinedText Text { get; private set; }
        public VisualElement Prompt { get; private set; }

        private void OnEnable()
        {
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            Root = documentRoot?.Q<VisualElement>(RootName);
            ImageA = documentRoot?.Q<VisualElement>(ImageAName);
            ImageB = documentRoot?.Q<VisualElement>(ImageBName);
            TextScroll = documentRoot?.Q<ScrollView>(TextScrollName);
            TextClip = documentRoot?.Q<VisualElement>(TextClipName);
            Text = documentRoot?.Q<OutlinedText>(TextName);
            Prompt = documentRoot?.Q<VisualElement>(PromptName);
            _skip = documentRoot?.Q<VisualElement>(SkipName);
            _skipLabel = documentRoot?.Q<OutlinedText>(SkipLabelName);
            _skipFill = documentRoot?.Q<VisualElement>(SkipFillName);

            IsBound = Root != null && ImageA != null && ImageB != null && TextScroll != null && TextClip != null &&
                      Text != null && Prompt != null && _skip != null && _skipLabel != null && _skipFill != null;

            if (!IsBound)
            {
                Debug.LogError("[CutsceneView] Missing one of the cutscene elements in the UXML.", this);
                return;
            }

            ResetVisuals();
        }

        // Nothing shown: images hidden, text empty, arrow and skip hint off, root fully opaque.
        public void ResetVisuals()
        {
            if (!IsBound) return;

            Root.style.opacity = 1f;
            ImageA.style.opacity = 0f;
            ImageB.style.opacity = 0f;
            Text.Text = string.Empty;
            TextClip.style.height = 0f;
            Prompt.style.opacity = 0f;
            HideSkipHint();
        }

        public void ShowSkipHint(string label)
        {
            if (!IsBound)
                return;

            _skipLabel.Text = label;
            _skip.style.display = DisplayStyle.Flex;
            SetSkipProgress(0f);
        }

        public void HideSkipHint()
        {
            if (IsBound)
                _skip.style.display = DisplayStyle.None;
        }

        // NOTE: Whole UI pixels only, so the fill edge stays crisp at every integer UI scale.
        public void SetSkipProgress(float progress)
        {
            if (!IsBound)
                return;

            var barWidth = _skipFill.parent.resolvedStyle.width;
            if (float.IsNaN(barWidth))
                barWidth = 0f;
            _skipFill.style.width = Mathf.Round(Mathf.Clamp01(progress) * barWidth);
        }
    }
}