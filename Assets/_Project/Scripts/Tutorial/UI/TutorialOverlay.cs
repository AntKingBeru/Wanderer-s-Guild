// The tutorial's screen layer (above the game screens): blocks the UI except allowed holes, spotlights the target and shows
// the instruction box.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Its own UIDocument (Sort 4, the shared Panel Settings) — it shares the panel with every screen, so target rects
    // (worldBound) are in its space and one pick covers all. Put the UIDocument FIRST on the GameObject.
    [RequireComponent(typeof(UIDocument))]
    public class TutorialOverlay : MonoBehaviour
    {
        private readonly List<VisualElement> _holes = new();
        private VisualElement _root;
        private TutorialBlocker _blocker;
        private TutorialSpotlight _spotlight;
        private TutorialBox _box;
        private TutorialInputFilter _filter;
        private VisualElement _panelRoot;
        private Action _onNext;
        private bool _isShown;

        public bool IsBound { get; private set; }

        public Button NextButton => _box?.NextButton;

        private void OnEnable()
        {
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            _root = documentRoot?.Q<VisualElement>("tutorial-root");
            _blocker = documentRoot?.Q<TutorialBlocker>("tutorial-blocker");
            _spotlight = TutorialSpotlight.Bind(documentRoot);
            _box = TutorialBox.Bind(documentRoot, gameObject, () => _onNext?.Invoke());

            IsBound = _root != null && _blocker != null && _spotlight != null && _box != null;
            if (!IsBound)
            {
                Debug.LogError("[TutorialOverlay] TutorialOverlay.uxml is missing tutorial-root, tutorial-blocker, the dims/frame or the box.", this);
                return;
            }

            Hide();
        }

        // NOTE: The keyboard filter goes on the shared panel root (every screen's navigation events pass it).
        private void Start()
        {
            if (!IsBound)
                return;

            _panelRoot = _root.panel?.visualTree;
            _filter = new TutorialInputFilter(() => _isShown, IsAllowedFocus);
            _panelRoot?.AddManipulator(_filter);
        }

        // NOTE: The panel root belongs to the PanelSettings asset and can outlive this scene.
        private void OnDestroy()
        {
            if (_panelRoot != null && _filter != null)
                _panelRoot.RemoveManipulator(_filter);
        }

        public void SetNextHandler(Action onNext) => _onNext = onNext;

        // Blocks the UI except the holes, spotlights the highlight (panel space) and shows the box beside it.
        public void Show(TutorialFrame frame, IReadOnlyList<VisualElement> holes)
        {
            if (!IsBound)
                return;

            SetShown(true);
            _holes.Clear();
            if (holes != null)
                _holes.AddRange(holes);
            _blocker.SetHoles(_holes);

            var settings = GameConfig.Instance.Tutorial;
            var screen = RootSize;
            var local = default(Rect);
            var hasHighlight = frame.HasHighlight && TryToLocal(frame.Highlight, out local);
            if (hasHighlight)
                _spotlight.Show(local, screen, settings.HighlightPadding, IsBlinkOn(settings.BlinkSeconds));
            else
                _spotlight.Hide();

            if (frame.Text == null)
            {
                _box.Hide();
                return;
            }

            _box.Show(frame.Text, frame.NextLabel);
            var size = _box.Size;
            var canPlace = size is { x: > 0f, y: > 0f } && screen is { x: > 0f, y: > 0f };
            var target = hasHighlight ? TutorialSpotlight.Framed(local, settings.HighlightPadding) : default;
            _box.Place(canPlace ? TutorialBoxPlacer.Place(size, screen, hasHighlight, target, settings) : Vector2.zero, canPlace);
        }

        // NOTE: Hidden = no blocker at all (the pause menu, scene loads, the tutorial over).
        public void Hide()
        {
            if (!IsBound)
                return;

            SetShown(false);
            _spotlight.Hide();
            _box.Hide();
            _holes.Clear();
            _blocker.SetHoles(null);
        }

        private void SetShown(bool shown)
        {
            _isShown = shown;
            _root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // NOTE: Holes take keyboard Submit too; so does the box's own Next button.
        private bool IsAllowedFocus(VisualElement focused)
        {
            return _box.Contains(focused) || _holes.Any(hole => hole == focused || hole.Contains(focused));
        }

        private Vector2 RootSize
        {
            get
            {
                var rect = _root.layout;
                return float.IsNaN(rect.width) || float.IsNaN(rect.height) ? Vector2.zero : rect.size;
            }
        }

        private bool TryToLocal(Rect panelRect, out Rect local)
        {
            local = _root.WorldToLocal(panelRect);
            return !float.IsNaN(local.x) && !float.IsNaN(local.width);
        }

        private static bool IsBlinkOn(float period) => Mathf.Repeat(Time.unscaledTime, period) < period * 0.5f;
    }
}