// The tutorial's keyboard gate: Submit and arrow presses reach the focused control only when the step allows that control.
using System;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Trickle-down on the shared panel root, so it runs before the focused control acts. Stopping propagation keeps the
    // control (a button's Submit, a cycler's arrows) from acting; panel MenuNavigation on the same root still moves focus.
    public sealed class TutorialInputFilter : Manipulator
    {
        private readonly Func<bool> _isActive;
        private readonly Func<VisualElement, bool> _allows;

        public TutorialInputFilter(Func<bool> isActive, Func<VisualElement, bool> allows)
        {
            _isActive = isActive;
            _allows = allows;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<NavigationSubmitEvent>(OnSubmit, TrickleDown.TrickleDown);
            target.RegisterCallback<NavigationMoveEvent>(OnMove, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<NavigationSubmitEvent>(OnSubmit, TrickleDown.TrickleDown);
            target.UnregisterCallback<NavigationMoveEvent>(OnMove, TrickleDown.TrickleDown);
        }

        private void OnSubmit(NavigationSubmitEvent evt) => Filter(evt);
        private void OnMove(NavigationMoveEvent evt) => Filter(evt);

        private void Filter(EventBase evt)
        {
            if (_isActive == null || !_isActive())
                return;

            var focused = target.focusController?.focusedElement as VisualElement;
            if (focused == null || !_allows(focused))
                evt.StopPropagation();
        }
    }
}