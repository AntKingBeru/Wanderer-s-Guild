// One step of a scripted tutorial: what it says and spotlights, what may be used, and when it's done.
using System;
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Plain data filled by TutorialStepBuilder. Done = Until (polled) if set, else a click on ClickTarget, else Next.
    // Nothing to show and no way to finish = instant (OnEnter runs, then the next step).
    public sealed class TutorialStep
    {
        private readonly List<ITutorialTarget> _holes = new();

        public Func<string> Text { get; internal set; } // The instruction (null = no box); read as the step starts.
        public string NextLabel { get; internal set; } // The Next button's label (null = no button).
        public ITutorialTarget Highlight { get; internal set; } // Spotlit (null = none).
        public ITutorialTarget ClickTarget { get; internal set; } // A click on its button finishes the step.
        public Func<bool> Ready { get; internal set; } // Extra condition before the step shows.
        public Func<bool> Until { get; internal set; } // Finishes the step when true.
        public Action OnEnter { get; internal set; } // Runs once as the step starts.
        public bool PausesTime { get; internal set; } = true; // Holds the game paused while the step is up.
        public bool SkipIfMissing { get; internal set; } // Passed over when its highlight never shows (a hidden line).
        public TutorialRules Rules { get; } = new(); // What the world and build mode allow meanwhile.

        // UI the player may use besides the instruction box.
        public IReadOnlyList<ITutorialTarget> Holes => _holes;

        public bool IsInstant => Text == null && NextLabel == null && ClickTarget == null && Until == null;

        internal void AddHole(ITutorialTarget target)
        {
            if (target != null && !_holes.Contains(target))
                _holes.Add(target);
        }
    }
}