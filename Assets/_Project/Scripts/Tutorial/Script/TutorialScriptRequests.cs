// Part 1 of the start tutorial: the desk → the request → posting the quest → the quest board → applications → approval.
using System.Collections.Generic;
using static WanderersGuild.TutorialStepBuilder;

namespace WanderersGuild
{
    // NOTE: Time stays paused throughout (steps pause by default) — the request can't expire and nobody signs up on their own.
    // One step per idea: a whole region is highlighted and summed up in a sentence.
    public static class TutorialScriptRequests
    {
        private const MenuPanelId Desk = MenuPanelId.ReceptionDesk;
        private const MenuPanelId Requests = MenuPanelId.RequestBoard;
        private const MenuPanelId Board = MenuPanelId.Quests;
        private const MenuPanelId Apps = MenuPanelId.Applications;
        private const string BackToGuildText = "Go back to the guild: close the screens (Esc works too).";

        public static void Append(List<TutorialStep> steps, TutorialContext c)
        {
            AppendRequest(steps, c);
            AppendQuestBoard(steps, c);
            AppendApplications(steps, c);
        }

        // "Go back to the guild": whichever Close is showing is spotlit and usable (Esc works too); done when no screen is open.
        public static TutorialStepBuilder BackToGuild(TutorialContext c, string text, params ITutorialTarget[] closes)
        {
            var close = c.Targets.EitherOf(closes);
            return Wait(text, () => c.NothingOpen).Highlight(close).Allow(close);
        }

        // Desk → requests → the request → Accept → the clipboard → Post → back to the guild.
        private static void AppendRequest(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(ClickWorld(c.Desk, "Townsfolk bring their troubles to the Reception Desk. Click it.", () => c.IsShowing(Desk)));
            steps.Add(Click(t.Panel(Desk, "desk-requests"), "Requests are jobs for your guild. Open them.")
                      .Until(() => c.IsShowing(Requests)));
            steps.Add(Click(t.PanelFirst(Requests, "request-row"), "Your first request! Select it."));
            steps.Add(Say("The job and who's asking, the rank and party size it suits, the reward, how long it waits, and what " +
                          "needs doing.")
                      .Highlight(t.Panel(Requests, "detail-content")));
            steps.Add(Click(t.Panel(Requests, "detail-accept"), "Accept it to write up a quest."));

            steps.Add(Say("The party's share of the reward: a bigger cut draws applicants, the rest goes to the guild.")
                      .Highlight(t.PanelParent(Requests, "clip-share")));
            steps.Add(Say("The lowest rank that may apply, how many must sign up, the gold split and how long they'll be away.")
                      .Highlight(t.Union(t.PanelParent(Requests, "clip-rank"), t.PanelParent(Requests, "clip-size"),
                                         t.Panel(Requests, "clip-split"), t.Panel(Requests, "clip-duration"))));
            steps.Add(Click(t.Panel(Requests, "clip-post"), "Looks good. Post it!").Until(() => c.QuestPosted));

            // NOTE: The board's Close does nothing while the clipboard is still stamping — wait until it has slid away.
            steps.Add(BackToGuild(c, "Posted! " + BackToGuildText, t.Panel(Requests, "board-close"), t.Panel(Desk, "desk-close"))
                      .When(() => t.IsHidden(Requests, "clipboard-layer")));
        }

        // The quest board in one look, then the starting party signs up.
        private static void AppendQuestBoard(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(ClickWorld(c.QuestBoard, "Posted quests hang on the Quest Board. Click it.", () => c.IsShowing(Board)));
            steps.Add(Say("There's your quest! Each one takes a slot: 3 to start, one more for every guild rank. Click a note any " +
                          "time for its details.")
                      .Highlight(t.Panel(Board, "qboard-grid")));
            steps.Add(BackToGuild(c, BackToGuildText, t.Panel(Board, "qboard-close")));
            steps.Add(Do(c.SignUpStartingParty));
            steps.Add(Say(() => $"{c.StartingPartyNames} have applied! Applications wait at the Reception Desk. A \"!\" above " +
                                "it means something needs you."));
        }

        // Applications: the party at a glance, approve, back to the guild.
        private static void AppendApplications(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(ClickWorld(c.Desk, "Click the Reception Desk.", () => c.IsShowing(Desk)));
            steps.Add(Click(t.Panel(Desk, "desk-applications"), "Open the applications.").Until(() => c.IsShowing(Apps)));
            steps.Add(Click(t.PanelFirst(Apps, "app-row"), "Quests with applicants are listed here. Select yours."));
            steps.Add(Say("Who applied, and the chance of success with them. Decline anyone you'd rather not send.")
                      .Highlight(t.Union(t.Panel(Apps, "app-applicants"), t.Panel(Apps, "app-time-chance"))));
            steps.Add(Click(t.Panel(Apps, "app-approve"), "The party is full. Approve it to send them off!").Until(() => c.QuestDeparted));
            steps.Add(BackToGuild(c, "They're on their way! " + BackToGuildText, t.Panel(Apps, "app-close"), t.Panel(Desk, "desk-close")));
        }
    }
}