using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class SliderStepNavigation : Manipulator
    {
        private readonly IReadOnlyList<SliderInt> _sliders;
        private readonly Func<bool> _isActive;
        private readonly int _step;

        public SliderStepNavigation(IReadOnlyList<SliderInt> sliders, Func<bool> isActive, int step)
        {
            _sliders = sliders;
            _isActive = isActive;
            _step = Mathf.Max(1, step);
        }

        protected override void RegisterCallbacksOnTarget() =>
            target.RegisterCallback<NavigationMoveEvent>(OnMove, TrickleDown.TrickleDown);

        protected override void UnregisterCallbacksFromTarget() =>
            target.UnregisterCallback<NavigationMoveEvent>(OnMove, TrickleDown.TrickleDown);

        // NOTE: Runs on the panel root during trickle-down, so the slider never receives the raw ±1 key step.
        private void OnMove(NavigationMoveEvent evt)
        {
            if (!_isActive())
                return;

            var direction = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => -1,
                NavigationMoveEvent.Direction.Right => 1,
                _ => 0
            };
            if (direction == 0)
                return;

            var slider = FocusedSlider();
            if (slider == null)
                return;

            slider.value = Mathf.Clamp(slider.value + direction * _step, slider.lowValue, slider.highValue);
            evt.StopPropagation();
            target.focusController?.IgnoreEvent(evt);
        }

        // NOTE: Focus may sit on the slider itself or on one of its inner parts.
        private SliderInt FocusedSlider()
        {
            if (target.focusController?.focusedElement is not VisualElement focused)
                return null;

            return _sliders.FirstOrDefault(slider => ReferenceEquals(slider, focused) || slider.Contains(focused));
        }
    }
}