using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class TextFieldNavigationGuard : Manipulator
    {
        private readonly Action _onArrowDown;

        /// <param name="onArrowDown">Runs when the real ↓ arrow key is pressed in the field (e.g. focus the next control).</param>
        public TextFieldNavigationGuard(Action onArrowDown) => _onArrowDown = onArrowDown;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        // NOTE: A navigation event doesn't say which key made it, so "S" and "↓" look the same here. Directional moves
        // are swallowed (typing is unaffected — it comes through key events); Tab (Next/Previous) still moves focus.
        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (evt.direction is NavigationMoveEvent.Direction.Next or NavigationMoveEvent.Direction.Previous)
                return;

            evt.StopPropagation();
            target.focusController?.IgnoreEvent(evt);
        }

        // NOTE: The real arrow key is told apart here, at the key level, so ↓ can still leave the field.
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.DownArrow)
                return;

            evt.StopPropagation();
            _onArrowDown?.Invoke();
        }
    }
}