// The guided tutorial's input rules, asked by world clicks, build mode and panel Esc. Inactive = everything allowed.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Static (like SaveBlockers) so systems need no tutorial reference; TutorialDirector applies each step's rules.
    public static class TutorialGate
    {
        private static TutorialRules _rules;

        public static bool IsActive => _rules != null;

        // The only world object that answers while active (null = none, or it doesn't exist right now).
        public static GuildInteractable WorldTarget =>
            _rules is { WorldTarget: not null } ? _rules.WorldTarget.Interactable : null;

        public static bool AllowsBuildModeEnter => _rules is not { BuildAccess: TutorialBuildAccess.Closed };
        public static bool KeepsBuildMode => _rules is { BuildAccess: TutorialBuildAccess.Kept };

        // NOTE: Clicking rooms (their upgrade / demolish card) is off for the whole tutorial.
        public static bool AllowsRoomPicking => _rules == null;

        // Whether this facility may be picked from the catalog — while active, only the step's pinned room.
        public static bool AllowsFacility(FacilityData data) =>
            _rules == null || (_rules.HasPlacement && data != null && data == _rules.Placement.Facility);

        // The spot this facility must go to (the ghost is pinned there). False = place freely.
        public static bool TryGetPlacement(FacilityData data, out TutorialPlacement placement)
        {
            placement = default;
            if (_rules is not { HasPlacement: true } || data == null || data != _rules.Placement.Facility)
                return false;

            placement = _rules.Placement;
            return true;
        }

        // Share of the usual build time for what's placed now (1 = normal).
        public static float BuildTimeScale => _rules is { HasPlacement: true } ? _rules.BuildTimeScale : 1f;

        // NOTE: Esc never closes or steps back in a tutorial screen; the pause menu and its screens keep theirs.
        public static bool BlocksCancel(MenuPanelId id) =>
            _rules != null && id is not (MenuPanelId.Pause or MenuPanelId.Options or MenuPanelId.QuitConfirmation);

        public static void Apply(TutorialRules rules) => _rules = rules;
        public static void Clear() => _rules = null;

        // NOTE: With domain reload off, stale rules from the last Play session would gate the next one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => _rules = null;
    }
}