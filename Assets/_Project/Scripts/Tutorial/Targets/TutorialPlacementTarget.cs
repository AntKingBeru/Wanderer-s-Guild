// The tutorial's "build this room here" target: the hammer, the room's card, its spot or Yes — whichever the player needs next.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: State-driven Composite — re-chosen on every use from build mode's state, so stepping back (Esc / right-click)
    // just moves the highlight back. The part taking over is emphasized (the card is scrolled into view).
    public sealed class TutorialPlacementTarget : ITutorialTarget
    {
        private readonly ITutorialTarget _hammer;
        private readonly ITutorialTarget _card;
        private readonly ITutorialTarget _spot;
        private readonly ITutorialTarget _confirmYes;
        private ITutorialTarget _current;
        private bool _isEmphasised;

        public TutorialPlacementTarget(TutorialPlacement placement, ITutorialTarget hammer, ITutorialTarget card, ITutorialTarget spot,
                                       ITutorialTarget confirmYes)
        {
            Placement = placement;
            _hammer = hammer;
            _card = card;
            _spot = spot;
            _confirmYes = confirmYes;
        }

        public TutorialPlacement Placement { get; }

        public bool IsAvailable => Current.IsAvailable;
        public VisualElement Element => Current.Element;
        public bool TryGetRect(out Rect panelRect) => Current.TryGetRect(out panelRect);

        public void SetEmphasis(bool on)
        {
            if (!on)
                _current?.SetEmphasis(false);

            _isEmphasised = on;
            _current = null;
            if (on)
                _ = Current; // NOTE: Picks and emphasizes the part that applies now.
        }

        private ITutorialTarget Current
        {
            get
            {
                var next = Choose();
                if (next == _current)
                    return next;

                if (_isEmphasised)
                {
                    _current?.SetEmphasis(false);
                    next.SetEmphasis(true);
                }

                _current = next;
                return next;
            }
        }

        private ITutorialTarget Choose()
        {
            if (!BuildModeController.HasInstance || !BuildModeController.Instance.IsActive)
                return _hammer;

            var build = BuildModeController.Instance;
            if (build.IsConfirming)
                return _confirmYes;
            return build.Selected == Placement.Facility ? _spot : _card;
        }
    }
}