// Binds the guild-naming UXML: name field, counter, error line, Confirm and the "Are you sure?" overlay; visuals only.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class GuildNamingView : MonoBehaviour
    {
        private const string RootName = "naming-root";
        private const string FieldName = "naming-field";
        private const string CounterName = "naming-counter";
        private const string ErrorName = "naming-error";
        private const string ConfirmName = "naming-confirm";

        private OutlinedText _counter;
        private OutlinedText _error;
        private List<VisualElement> _focusables;
        private bool _isInitialized;

        public bool IsBound { get; private set; }
        public bool IsInteractable { get; private set; }
        public VisualElement Root { get; private set; }
        public TextField Field { get; private set; }
        public Button Confirm { get; private set; }

        // The "Are you sure?" overlay shown after the player confirms a name.
        public InlineConfirmation Confirmation { get; private set; }

        // Field, then Confirm — the vertical focus order.
        public IReadOnlyList<VisualElement> Focusables => _focusables;

        // The panel's top element, where navigation manipulators go.
        public VisualElement PanelRoot => Root?.panel?.visualTree;

        // True while the name field (or its inner text element) has keyboard focus.
        public bool IsFieldFocused =>
            IsBound && Root.focusController?.focusedElement is VisualElement focused &&
            (ReferenceEquals(focused, Field) || Field.Contains(focused));

        public float Opacity
        {
            set
            {
                if (IsBound)
                    Root.style.opacity = value;
            }
        }

        private void OnEnable()
        {
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            Root = documentRoot?.Q<VisualElement>(RootName);
            Field = documentRoot?.Q<TextField>(FieldName);
            _counter = documentRoot?.Q<OutlinedText>(CounterName);
            _error = documentRoot?.Q<OutlinedText>(ErrorName);
            Confirm = documentRoot?.Q<Button>(ConfirmName);
            Confirmation = documentRoot != null ? InlineConfirmation.Bind(documentRoot) : null;

            IsBound = Root != null && Field != null && _counter != null && _error != null && Confirm != null && Confirmation != null;
            if (!IsBound)
            {
                Debug.LogError("[GuildNamingView] Missing one of the naming elements (or the confirm overlay) in the UXML.", this);
                return;
            }

            _focusables = new List<VisualElement> { Field, Confirm };
            SetVisible(false);
            SetInteractable(false);
        }

        // Wires the overlay and adds pixel-button setup + hover/focus feedback to every button. Runs once.
        public void Initialize(GameObject linkTarget)
        {
            if (!IsBound || _isInitialized)
                return;

            _isInitialized = true;
            Confirmation.Initialize();

            SetUpButton(Confirm, linkTarget);
            foreach (var button in Confirmation.Buttons)
                SetUpButton(button, linkTarget);
        }

        // Empty field, counter at 0, no error, Confirm disabled, overlay hidden.
        public void ResetView(int maxLength)
        {
            if (!IsBound)
                return;

            Confirmation.HideImmediate();
            Field.SetValueWithoutNotify(string.Empty);
            SetCounter(0, maxLength);
            SetError(null);
            Confirm.SetEnabled(false);
        }

        public void SetVisible(bool visible)
        {
            if (!IsBound)
                return;

            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                Root.style.opacity = 0f;
        }

        // NOTE: Locks the field and Confirm only (read-only, not disabled, so nothing grays out); the overlay's own
        // buttons stay usable, so the prompt can be answered while the panel behind it is locked.
        public void SetInteractable(bool interactable)
        {
            if (!IsBound)
                return;

            IsInteractable = interactable;
            Field.isReadOnly = !interactable;
            Confirm.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
            Confirm.focusable = interactable;

            if (!interactable && Root.focusController?.focusedElement is VisualElement focused &&
                (ReferenceEquals(focused, Field) || Field.Contains(focused) || ReferenceEquals(focused, Confirm)))
                focused.Blur();
        }

        public void SetCounter(int length, int maxLength)
        {
            if (IsBound)
                _counter.Text = $"{length}/{maxLength}";
        }

        // Null or empty hides the error line.
        public void SetError(string message)
        {
            if (!IsBound)
                return;

            var show = !string.IsNullOrEmpty(message);
            _error.Text = show ? message : string.Empty;
            _error.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetUpButton(Button button, GameObject linkTarget)
        {
            button.RemoveFromClassList(Button.ussClassName); // NOTE: Default theme styles would override our frames.
            button.AddManipulator(new MenuButtonFeedback(linkTarget));
        }
    }
}