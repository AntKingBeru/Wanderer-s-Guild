using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ScrollKeyNavigation : Manipulator
    {
        private readonly ScrollView _scroll;
        private readonly Func<bool> _isActive;
        private readonly float _step;

        public ScrollKeyNavigation(ScrollView scroll, Func<bool> isActive, float step)
        {
            _scroll = scroll;
            _isActive = isActive;
            _step = step;
        }

        protected override void RegisterCallbacksOnTarget() =>
            target.RegisterCallback<NavigationMoveEvent>(OnMove, TrickleDown.TrickleDown);

        protected override void UnregisterCallbacksFromTarget() =>
            target.UnregisterCallback<NavigationMoveEvent>(OnMove, TrickleDown.TrickleDown);

        // NOTE: All moves are consumed while active, so the default focus ring can't pull focus off the panel.
        private void OnMove(NavigationMoveEvent evt)
        {
            if (!_isActive())
                return;

            var delta = evt.direction switch
            {
                NavigationMoveEvent.Direction.Up => -_step,
                NavigationMoveEvent.Direction.Down => _step,
                _ => 0f
            };

            if (delta != 0f)
                ScrollBy(delta);

            evt.StopPropagation();
            target.focusController?.IgnoreEvent(evt);
        }

        // NOTE: Clamped by hand, so repeated presses past the end never build up an offset to "unwind".
        private void ScrollBy(float delta)
        {
            var maxOffset = Mathf.Max(0f, _scroll.contentContainer.layout.height - _scroll.contentViewport.layout.height);
            var y = Mathf.Clamp(_scroll.scrollOffset.y + delta, 0f, maxOffset);
            _scroll.scrollOffset = new Vector2(_scroll.scrollOffset.x, y);
        }
    }
}