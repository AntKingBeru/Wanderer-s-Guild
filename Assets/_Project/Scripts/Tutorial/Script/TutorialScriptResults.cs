// Part 3 of the start tutorial: the quest comes back a success (and guild rank), the Lounge → an adventurer's card in one
// look, and the step that completes the tutorial.
using System.Collections.Generic;
using static WanderersGuild.TutorialStepBuilder;

namespace WanderersGuild
{
    // NOTE: The quest is resolved (success, no injuries) only now — it was held through the building part. Time runs until the
    // roster opens, so the party can walk back in.
    public static class TutorialScriptResults
    {
        private const MenuPanelId Roster = MenuPanelId.Roster;

        public static void Append(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(Do(c.ResolveQuest));
            steps.Add(Say(() => $"{c.StartingPartyNames} are back, and the quest was a success! The guild earned its share and " +
                                "guild points, which fill this bar: each new rank brings better requests, more quest slots and " +
                                "new rooms.")
                      .Highlight(t.HudParent("rank-badge")).TimeRuns());
            steps.Add(ClickWorld(c.Lounge, "Your adventurers grew too. Click the Lounge to see them.", () => c.IsShowing(Roster)).TimeRuns());

            steps.Add(Click(t.PanelFirst(Roster, "roster-row"), "Select an adventurer. You can sort and filter the list from the top."));
            steps.Add(Say("Their card: who they are, rank and level, stats, gear and what they're doing. The buttons give them a " +
                          "room, promote them or break up their party.")
                      .Highlight(t.PanelParent(Roster, "adv-content")));

            // NOTE: The last step is the only thing that completes the tutorial.
            steps.Add(TutorialScriptRequests.BackToGuild(c, "Go back to the guild.", t.Panel(Roster, "roster-close")));
            steps.Add(Say("That's the basics! Requests arrive every morning, and new adventurers come as your reputation grows. " +
                          "The guild is yours, Guild Master!").Next("Finish"));
            steps.Add(Do(c.CompleteTutorial));
        }
    }
}