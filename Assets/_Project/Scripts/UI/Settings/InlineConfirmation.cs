using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class InlineConfirmation
    {
        // NOTE: One overlay per document; markup and styles are shared (PixelTheme.uss › confirm-*).
        private const string RootName = "confirm-overlay";
        private const string MessageName = "confirm-message";
        private const string NoName = "confirm-no";
        private const string YesName = "confirm-yes";

        private readonly VisualElement _root;
        private readonly OutlinedText _message;
        private readonly Button _no;
        private readonly Button _yes;
        private readonly List<Button> _buttons;
        private Action _onConfirm;
        private Action _onCancelled;
        private Action _onClosed;

        public bool IsOpen { get; private set; }

        // Cancel choice first (left), confirm choice second (right) — the horizontal focus order.
        public IReadOnlyList<Button> Buttons => _buttons;

        private InlineConfirmation(VisualElement root, OutlinedText message, Button no, Button yes)
        {
            _root = root;
            _message = message;
            _no = no;
            _yes = yes;
            _buttons = new List<Button> { no, yes };
        }

        // Finds the overlay, then its parts inside it. Returns null if anything is missing.
        public static InlineConfirmation Bind(VisualElement documentRoot)
        {
            var root = documentRoot.Q<VisualElement>(RootName);
            if (root == null)
                return null;

            var message = root.Q<OutlinedText>(MessageName);
            var no = root.Q<Button>(NoName);
            var yes = root.Q<Button>(YesName);

            return message != null && no != null && yes != null
                ? new InlineConfirmation(root, message, no, yes)
                : null;
        }

        public void Initialize()
        {
            _no.clicked += Cancel;
            _yes.clicked += Confirm;
        }

        /// <param name="message"></param>
        /// <param name="onConfirm">Runs when the confirmmation (right) choice is picked.</param>
        /// <param name="onClosed">Runs after the overlay closes, however it closed. Safe to open the next overlay from here.</param>
        /// <param name="confirmLabel"></param>
        /// <param name="cancelLabel"></param>
        /// <param name="focusConfirm">Pre-select the confirmmation choice instead of the cancel choice.</param>
        /// <param name="onCancelled">Runs when the cancel (left) choice or a timeout answers.</param>
        public void Show(string message, Action onConfirm, Action onClosed,
                         string confirmLabel = "Yes", string cancelLabel = "No",
                         bool focusConfirm = false, Action onCancelled = null)
        {
            _message.Text = message;
            SetLabel(_yes, confirmLabel);
            SetLabel(_no, cancelLabel);

            _onConfirm = onConfirm;
            _onCancelled = onCancelled;
            _onClosed = onClosed;
            IsOpen = true;
            _root.style.display = DisplayStyle.Flex;

            // NOTE: Focus is requested next frame — an element that was just displayed can't always take focus this frame.
            var first = focusConfirm ? _yes : _no;
            first.schedule.Execute(() => { if (IsOpen) first.Focus(); });
        }

        // Updates the message while open (e.g. a countdown).
        public void SetMessage(string message) => _message.Text = message;

        // Answers with the cancel (left) choice.
        public void Cancel() => Close(_onCancelled);

        // Closes without either answer (e.g. Esc on "Apply changes?" = stay); only onClosed runs.
        public void Dismiss() => Close(null);

        // Hides without running any callbacks (e.g. when the whole panel is reopened).
        public void HideImmediate()
        {
            IsOpen = false;
            _root.style.display = DisplayStyle.None;
            _onConfirm = null;
            _onCancelled = null;
            _onClosed = null;
        }

        private void Confirm() => Close(_onConfirm);

        // NOTE: Hides first, then runs the answer, then onClosed — a follow-up overlay opened from onClosed is never
        // closed or overridden by this one's leftovers.
        private void Close(Action answer)
        {
            if (!IsOpen)
                return;

            var closed = _onClosed;
            HideImmediate();

            answer?.Invoke();
            closed?.Invoke();
        }

        private static void SetLabel(Button button, string label)
        {
            var text = button.Q<OutlinedText>();
            if (text != null)
                text.Text = label;
        }
    }
}