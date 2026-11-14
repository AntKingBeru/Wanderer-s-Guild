// Part 3 of the start tutorial: the quest comes back a success, guild rank, then the lounge → an adventurer's card part by
// part, and the step that completes the tutorial.
using System.Collections.Generic;
using static WanderersGuild.TutorialStepBuilder;

namespace WanderersGuild
{
    // NOTE: The quest is resolved (success, no injuries) only now — it was held through the building part. Time runs until the
    // roster opens, so the party can walk back in. Lines that are hidden (no party yet, no room) are skipped.
    public static class TutorialScriptResults
    {
        private const MenuPanelId Roster = MenuPanelId.Roster;

        public static void Append(List<TutorialStep> steps, TutorialContext c)
        {
            AppendReturn(steps, c);
            AppendCard(steps, c);
            AppendFinish(steps, c);
        }

        private static void AppendReturn(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(Do(c.ResolveQuest));
            steps.Add(Say(() => $"{c.StartingPartyNames} are back, and the quest was a success! The guild got its share of the " +
                                "reward, and guild points.").TimeRuns());
            steps.Add(Say("Guild points fill this bar. Each new guild rank brings harder, better-paying requests, more quest slots " +
                          "and new rooms.")
                      .Highlight(t.HudParent("rank-badge")).TimeRuns());
            steps.Add(ClickWorld(c.Lounge, "Your adventurers grew too. Click the Lounge to see them.", () => c.IsShowing(Roster)).TimeRuns());
        }

        // The adventurer card, top to bottom.
        private static void AppendCard(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(Click(t.PanelFirst(Roster, "roster-row"), "Select an adventurer."));
            steps.Add(Say("Their full name.").Highlight(t.Panel(Roster, "adv-name")));
            steps.Add(Say("Species and class. Each class shines at different quests.").Highlight(t.PanelParent(Roster, "adv-class-icon")));
            steps.Add(Say("Their rank and level.").Highlight(t.Panel(Roster, "adv-rank-level")));
            steps.Add(Say("Experience from quests. Each level makes them stronger.").Highlight(t.PanelParent(Roster, "adv-xp-track")));
            steps.Add(Say("Rank progress. Fill it and they can take an exam for the next rank.").Highlight(t.PanelParent(Roster, "adv-rankp-track")));
            steps.Add(Say("Their stats, which decide how well they do on quests.")
                      .Highlight(t.Union(t.Panel(Roster, "adv-stats-top"), t.Panel(Roster, "adv-stats-bottom"))));
            steps.Add(Say("The gold they've earned. They spend it on better gear.").Highlight(t.Panel(Roster, "adv-wealth")));
            steps.Add(Say("The kinds of quests they're best at.").Highlight(t.Panel(Roster, "adv-affinities")));
            steps.Add(Say("Their equipment.").Highlight(t.Panel(Roster, "adv-gear")));
            steps.Add(Say("What they're doing right now.").Highlight(t.Panel(Roster, "adv-status")));
            steps.Add(Say("Their party. Adventurers who keep succeeding together team up.").Highlight(t.Panel(Roster, "adv-party")).SkipIfMissing());
            steps.Add(Say("The bedroom they sleep in.").Highlight(t.Panel(Roster, "adv-room")).SkipIfMissing());
            steps.Add(Say("Move them to another bedroom, or promote them once they're ready.").Highlight(t.PanelParent(Roster, "adv-move")));
            steps.Add(Say("Disband breaks up their party.").Highlight(t.Panel(Roster, "adv-disband")));
        }

        // NOTE: The last step is the only thing that completes the tutorial.
        private static void AppendFinish(List<TutorialStep> steps, TutorialContext c)
        {
            steps.Add(Click(c.Targets.Panel(Roster, "roster-close"), "Close the roster.").Until(() => c.NothingOpen));
            steps.Add(Say("That's the basics! Requests arrive every morning, and new adventurers come as your reputation grows. " +
                          "The guild is yours, Guild Master!").Next("Finish"));
            steps.Add(Do(c.CompleteTutorial));
        }
    }
}