// What the player may do during one tutorial step: which world object answers clicks, what build mode allows, and the one
// room it may build (pinned to its spot).
namespace WanderersGuild
{
    // NOTE: Filled by TutorialStepBuilder, read through TutorialGate. A fresh instance allows nothing (no world object, build
    // mode closed, nothing to build) — the director uses one between steps.
    public sealed class TutorialRules
    {
        // The only world object that answers hover/clicks (null = none) — resolved when asked, since rooms spawn at runtime.
        public TutorialWorldTarget WorldTarget { get; internal set; }

        public TutorialBuildAccess BuildAccess { get; internal set; } = TutorialBuildAccess.Closed;

        // The one room the step may build, and where (false = none).
        public bool HasPlacement { get; private set; }
        public TutorialPlacement Placement { get; private set; }

        // Share of the usual build time for that room (the guided 0.25).
        public float BuildTimeScale { get; private set; } = 1f;

        internal void SetPlacement(TutorialPlacement placement, float buildTimeScale)
        {
            HasPlacement = placement.Facility != null;
            Placement = placement;
            BuildTimeScale = buildTimeScale;
        }
    }
}