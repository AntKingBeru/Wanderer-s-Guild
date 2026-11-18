// Which adventurers pass the roster's filter flags (every ticked box adds its group), with labels and empty messages.
namespace WanderersGuild
{
    // NOTE: Union — an adventurer is listed if ANY ticked box matches them (no box ticked = everyone). "Busy" = signed up or
    // away (not idle, not injured); "Ready" = the colored names (ready for a promotion exam or a class trial, gold aside).
    public static class RosterFilterRules
    {
        public static bool Passes(Adventurer adventurer, RosterFilter filter)
        {
            if (filter == RosterFilter.None)
                return true;
            if ((filter & StatusFlagOf(adventurer)) != RosterFilter.None)
                return true;

            return (filter & RosterFilter.Ready) != RosterFilter.None &&
                   RosterReadinessRules.Of(adventurer) != RosterReadiness.None;
        }

        // One flag's name ("All" for none).
        public static string Label(RosterFilter flag) => flag switch
        {
            RosterFilter.Idle => "Idle",
            RosterFilter.Busy => "Busy",
            RosterFilter.Injured => "Injured",
            RosterFilter.Ready => "Ready",
            _ => "All"
        };

        // Shown in the list and the details when nobody passes.
        public static string EmptyMessage(RosterFilter filter) => filter switch
        {
            RosterFilter.None => "No adventurers yet.",
            RosterFilter.Idle => "Nobody is idle.",
            RosterFilter.Busy => "Nobody is busy.",
            RosterFilter.Injured => "Nobody is injured.",
            RosterFilter.Ready => "Nobody is ready to move up.",
            _ => "Nobody matches the filter."
        };

        private static RosterFilter StatusFlagOf(Adventurer adventurer) => adventurer.Status switch
        {
            AdventurerStatus.Idle => RosterFilter.Idle,
            AdventurerStatus.Injured => RosterFilter.Injured,
            _ => RosterFilter.Busy
        };
    }
}