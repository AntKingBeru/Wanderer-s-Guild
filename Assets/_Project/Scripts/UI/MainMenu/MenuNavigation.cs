using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class MenuNavigation : Manipulator
    {
        private readonly IReadOnlyList<VisualElement> _elements;
        private readonly Func<bool> _isInteractive;
        private readonly Action _onCancel;
        private readonly NavigationAxis _axis;
        private readonly bool _passThroughCrossAxis;

        /// <param name="elements">Focus order. Maybe a list that is changed later (e.g. per Options tab).</param>
        /// <param name="isInteractive">Gate checked on every event. While false, events pass through untouched.</param>
        /// <param name="onCancel">Run on Esc while interactive. Pass null to let another handler own Esc.</param>
        /// <param name="axis">Which arrow keys move focus. Tab and Shift+Tab always work.</param>
        /// <param name="passThroughCrossAxis">True: arrows on the other axis reach the focused control (sliders, tab rows).</param>
        public MenuNavigation(IReadOnlyList<VisualElement> elements, Func<bool> isInteractive, Action onCancel,
            NavigationAxis axis = NavigationAxis.Vertical, bool passThroughCrossAxis = false)
        {
            _elements = elements;
            _isInteractive = isInteractive;
            _onCancel = onCancel;
            _axis = axis;
            _passThroughCrossAxis = passThroughCrossAxis;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<NavigationMoveEvent>(OnMove, TrickleDown.TrickleDown);
            target.RegisterCallback<NavigationCancelEvent>(OnCancel, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<NavigationMoveEvent>(OnMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<NavigationCancelEvent>(OnCancel, TrickleDown.TrickleDown);
        }

        // NOTE: While interactive, moves are consumed so the default focus ring can't land on dividers, disabled
        // controls or another screen. Cross-axis moves pass through only when passThroughCrossAxis is set.
        private void OnMove(NavigationMoveEvent evt)
        {
            if (!_isInteractive())
                return;

            var step = StepFor(evt.direction);
            if (step == 0 && _passThroughCrossAxis)
                return;

            if (step != 0)
                FocusNext(step);

            evt.StopPropagation();
            target.focusController?.IgnoreEvent(evt);
        }

        private void OnCancel(NavigationCancelEvent evt)
        {
            if (!_isInteractive() || _onCancel == null)
                return;

            _onCancel.Invoke();
            evt.StopPropagation();
        }

        private int StepFor(NavigationMoveEvent.Direction direction) => direction switch
        {
            NavigationMoveEvent.Direction.Previous => -1,
            NavigationMoveEvent.Direction.Next => 1,
            NavigationMoveEvent.Direction.Up when _axis == NavigationAxis.Vertical => -1,
            NavigationMoveEvent.Direction.Down when _axis == NavigationAxis.Vertical => 1,
            NavigationMoveEvent.Direction.Left when _axis == NavigationAxis.Horizontal => -1,
            NavigationMoveEvent.Direction.Right when _axis == NavigationAxis.Horizontal => 1,
            _ => 0
        };

        private void FocusNext(int step)
        {
            var count = _elements.Count;
            if (count == 0)
                return;

            // With nothing focused, forward starts at the first element and backward at the last.
            var current = IndexOfFocused();
            var index = current >= 0 ? current : (step > 0 ? -1 : 0);

            for (var i = 0; i < count; i++)
            {
                index = (index + step + count) % count;
                var candidate = _elements[index];

                if (candidate.enabledInHierarchy && candidate.focusable)
                {
                    candidate.Focus();
                    return;
                }
            }
        }

        // NOTE: Also matches when focus sits on a control's inner part (e.g. inside a slider).
        private int IndexOfFocused()
        {
            if (target.focusController?.focusedElement is not VisualElement focused)
                return -1;

            for (var i = 0; i < _elements.Count; i++)
            {
                var element = _elements[i];
                if (ReferenceEquals(element, focused) || element.Contains(focused))
                    return i;
            }
            return -1;
        }
    }
}