using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class MenuPanel : MonoBehaviour
    {
        private const string RootName = "panel-root";
        private const string WindowName = "panel-window";

        [SerializeField] private MenuPanelId id;

        [Tooltip("On: the menu column and logo hide while this panel is open. Off: the menu stays visible, dimmed behind.")]
        [SerializeField] private bool hidesMenu = true;

        private readonly List<Manipulator> _panelManipulators = new();
        private PanelTransitionAnimator _transition;
        private VisualElement _panelRoot;
        private bool _isAnimating;
        private bool _isInitialized;

        public MenuPanelId Id => id;
        public bool HidesMenu => hidesMenu;
        public bool IsBound { get; private set; }
        public bool IsOpen { get; private set; }

        protected IMenuPanelHost Host { get; private set; }
        protected VisualElement Root { get; private set; }
        protected VisualElement Window { get; private set; }

        // True only while fully open (not mid-transition). Every panel input handler checks this.
        protected bool IsInteractable => IsOpen && !_isAnimating;

        // The panel's buttons. They get pixel-button setup and are blocked while the panel is closed.
        protected abstract IReadOnlyList<Button> Buttons { get; }
        
        // Buttons inside clipped lists (e.g. save slots): same setup and gating, but no focus pop.
        protected virtual IReadOnlyList<Button> ListButtons => Array.Empty<Button>();

        // The element focused once the panel finishes opening.
        protected abstract Focusable DefaultFocus { get; }

        // NOTE: Subclasses must not declare OnEnable; override OnBind instead.
        private void OnEnable()
        {
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            Root = documentRoot?.Q<VisualElement>(RootName);
            Window = documentRoot?.Q<VisualElement>(WindowName);
            IsBound = Root != null && Window != null && OnBind(documentRoot);

            if (!IsBound)
                Debug.LogError($"[MenuPanel] '{name}' is missing panel-root, panel-window or one of its own elements.", this);
        }

        // Called once by the host after every panel is bound.
        public void Initialize(IMenuPanelHost host)
        {
            if (_isInitialized || !IsBound)
                return;

            _isInitialized = true;
            Host = host;
            _transition = new PanelTransitionAnimator(gameObject);
            _panelRoot = Root.panel?.visualTree;

            foreach (var button in Buttons)
                SetUpButton(button, pop: true);

            foreach (var button in ListButtons)
                SetUpButton(button, pop: false);

            SetButtonsInteractable(false);
            _panelRoot?.RegisterCallback<NavigationCancelEvent>(OnCancelEvent, TrickleDown.TrickleDown);
            OnInitialize();
        }

        protected virtual void OnDestroy()
        {
            // NOTE: The panel root belongs to the PanelSettings asset and can outlive this scene.
            if (_panelRoot == null)
                return;

            foreach (var manipulator in _panelManipulators)
                _panelRoot.RemoveManipulator(manipulator);

            _panelRoot.UnregisterCallback<NavigationCancelEvent>(OnCancelEvent, TrickleDown.TrickleDown);
        }

        public async Awaitable OpenAsync(CancellationToken token)
        {
            if (!IsBound || IsOpen)
                return;

            IsOpen = true;
            _isAnimating = true;
            Root.style.display = DisplayStyle.Flex;
            OnOpening();

            try
            {
                await _transition.PlayAsync(Root, Window, true, token);
            }
            finally
            {
                _isAnimating = false;
            }

            SetButtonsInteractable(true);
            DefaultFocus?.Focus();
        }

        public async Awaitable CloseAsync(CancellationToken token)
        {
            if (!IsBound || !IsOpen)
                return;

            _isAnimating = true;
            SetButtonsInteractable(false);

            try
            {
                await _transition.PlayAsync(Root, Window, false, token);
            }
            finally
            {
                _isAnimating = false;
                IsOpen = false;
                Root.style.display = DisplayStyle.None;
            }
        }

        // Query the panel's own elements. Return false if any required one is missing.
        protected abstract bool OnBind(VisualElement documentRoot);

        // Wire clicks and navigation. Runs once, after pixel-button setup.
        protected virtual void OnInitialize() { }

        // Runs right before the open transition (e.g. reset a scroll position).
        protected virtual void OnOpening() { }

        // Esc while open. Default: close this panel.
        protected virtual void OnCancel() => Host?.Close(Id);

        // Adds a manipulator to the panel root and removes it automatically on destroy.
        protected void AddPanelManipulator(Manipulator manipulator)
        {
            if (_panelRoot == null)
                return;

            _panelRoot.AddManipulator(manipulator);
            _panelManipulators.Add(manipulator);
        }

        private void SetUpButton(Button button, bool pop)
        {
            button.RemoveFromClassList(Button.ussClassName); // NOTE: Default theme button styles would override our frames.
            button.AddManipulator(new MenuButtonFeedback(gameObject, pop));
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (!interactable)
                BlurIfFocusedInside();

            SetInteractable(Buttons, interactable);
            SetInteractable(ListButtons, interactable);
        }

        private static void SetInteractable(IReadOnlyList<Button> buttons, bool interactable)
        {
            foreach (var button in buttons)
            {
                button.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
                button.focusable = interactable;
            }
        }

        private void BlurIfFocusedInside()
        {
            if (Root.focusController?.focusedElement is VisualElement focused && Root.Contains(focused))
                focused.Blur();
        }

        private void OnCancelEvent(NavigationCancelEvent evt)
        {
            if (!IsInteractable)
                return;

            evt.StopPropagation();
            OnCancel();
        }
    }
}