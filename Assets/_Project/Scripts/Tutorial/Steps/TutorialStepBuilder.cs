// Builds tutorial steps fluently (Builder): Say / Click / ClickWorld / Wait / Do, then options like Highlight, When, Until.
using System;

namespace WanderersGuild
{
    // NOTE: Converts implicitly to TutorialStep, so a script writes steps.Add(Say("…").Highlight(target)).
    public sealed class TutorialStepBuilder
    {
        private const string DefaultNextLabel = "Next";

        private readonly TutorialStep _step = new();

        private TutorialStepBuilder() { }

        // An explanation, dismissed with Next.
        public static TutorialStepBuilder Say(string text) => Say(() => text);

        // An explanation whose text is worked out as the step starts (names, counts).
        public static TutorialStepBuilder Say(Func<string> text) => new TutorialStepBuilder().Text(text).Next(DefaultNextLabel);

        // "Click this": spotlit and the only usable UI; done when its button is clicked (or when Until says so).
        public static TutorialStepBuilder Click(ITutorialTarget target, string text)
        {
            var builder = new TutorialStepBuilder().Text(() => text).Highlight(target).Allow(target);
            builder._step.ClickTarget = target;
            return builder;
        }

        // "Click this in the world": outlined, spotlit and the only world object that answers; done when Until says so.
        public static TutorialStepBuilder ClickWorld(TutorialWorldTarget target, string text, Func<bool> until)
        {
            var builder = new TutorialStepBuilder().Text(() => text).Highlight(target).Until(until);
            builder._step.Rules.WorldTarget = target;
            return builder;
        }
        
        // "Build this room here": one highlight follows the hammer → its card → the spot → Yes. Build mode stays open, only
        // this room can be picked, the ghost is pinned to the spot, and it builds at the guided timescale.
        public static TutorialStepBuilder Place(TutorialPlacementTarget target, string text, Func<bool> until)
        {
            var builder = new TutorialStepBuilder().Text(() => text).Highlight(target).Allow(target).Until(until)
                .BuildMode(TutorialBuildAccess.Kept);
            builder._step.Rules.SetPlacement(target.Placement, GameConfig.Instance.Tutorial.GuidedBuildTimeScale);
            return builder;
        }

        // Waits for something to happen while the text is up (no Next).
        public static TutorialStepBuilder Wait(string text, Func<bool> until) => new TutorialStepBuilder().Text(() => text).Until(until);

        // Runs an action and moves straight on (nothing shown).
        public static TutorialStepBuilder Do(Action action) => new TutorialStepBuilder().OnEnter(action);

        public TutorialStepBuilder Text(Func<string> text)
        {
            _step.Text = text;
            return this;
        }

        public TutorialStepBuilder Next(string label)
        {
            _step.NextLabel = label;
            return this;
        }

        public TutorialStepBuilder Highlight(ITutorialTarget target)
        {
            _step.Highlight = target;
            return this;
        }

        // Lets the player use this UI too (a hole in the blocker).
        public TutorialStepBuilder Allow(ITutorialTarget target)
        {
            _step.AddHole(target);
            return this;
        }

        public TutorialStepBuilder When(Func<bool> ready)
        {
            _step.Ready = ready;
            return this;
        }

        public TutorialStepBuilder Until(Func<bool> done)
        {
            _step.Until = done;
            return this;
        }

        public TutorialStepBuilder OnEnter(Action action)
        {
            _step.OnEnter += action;
            return this;
        }

        public TutorialStepBuilder TimeRuns()
        {
            _step.PausesTime = false;
            return this;
        }

        public TutorialStepBuilder BuildMode(TutorialBuildAccess access)
        {
            _step.Rules.BuildAccess = access;
            return this;
        }

        public TutorialStepBuilder SkipIfMissing()
        {
            _step.SkipIfMissing = true;
            return this;
        }

        public static implicit operator TutorialStep(TutorialStepBuilder builder) => builder._step;
    }
}