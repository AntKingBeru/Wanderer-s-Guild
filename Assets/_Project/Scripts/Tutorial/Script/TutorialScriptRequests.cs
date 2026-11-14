// Part 1 of the start tutorial: reception desk → the request → posting the quest → the quest board → applications → approval.
using System.Collections.Generic;
using static WanderersGuild.TutorialStepBuilder;

namespace WanderersGuild
{
    // NOTE: Time stays paused throughout (steps pause by default) — the request can't expire and nobody signs up on their own.
    public static class TutorialScriptRequests
    {
        private const MenuPanelId Desk = MenuPanelId.ReceptionDesk;
        private const MenuPanelId Requests = MenuPanelId.RequestBoard;
        private const MenuPanelId Board = MenuPanelId.Quests;
        private const MenuPanelId Apps = MenuPanelId.Applications;

        public static void Append(List<TutorialStep> steps, TutorialContext c)
        {
            AppendRequest(steps, c);
            AppendQuestBoard(steps, c);
            AppendApplications(steps, c);
        }

        // Desk → requests → read the request → the clipboard → Post → back out.
        private static void AppendRequest(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(Say("Welcome, Guild Master! Townsfolk bring their troubles to the reception desk."));
            steps.Add(ClickWorld(c.Desk, "Click the Reception Desk.", () => c.IsShowing(Desk)));
            steps.Add(Click(t.Panel(Desk, "desk-requests"), "Requests are jobs brought to your guild. Open them.")
                      .Until(() => c.IsShowing(Requests)));
            steps.Add(Click(t.PanelFirst(Requests, "request-row"), "Your first request! Select it to read it."));
            steps.Add(Say("The job, its type and who's asking.")
                      .Highlight(t.Union(t.Panel(Requests, "detail-title"), t.Panel(Requests, "detail-category"))));
            steps.Add(Say("The rank it calls for, and the party size it suits.").Highlight(t.Panel(Requests, "detail-rank")));
            steps.Add(Say("The reward. You choose how it's split with the party.").Highlight(t.Panel(Requests, "detail-reward")));
            steps.Add(Say("Requests don't wait forever: answer before this runs out.").Highlight(t.Panel(Requests, "detail-time")));
            steps.Add(Say("What needs doing.").Highlight(t.Panel(Requests, "detail-objective-scroll")));
            steps.Add(Click(t.Panel(Requests, "detail-accept"), "Accept it to write up a quest."));

            steps.Add(Say("Reward share: the party's cut. A bigger cut draws applicants; the rest goes to the guild.")
                      .Highlight(t.PanelParent(Requests, "clip-share")));
            steps.Add(Say("Minimum rank: the lowest rank that may apply.").Highlight(t.PanelParent(Requests, "clip-rank")));
            steps.Add(Say("Party size: how many must sign up before they can set out.").Highlight(t.PanelParent(Requests, "clip-size")));
            steps.Add(Say("The gold split, and about how long the party will be away.")
                      .Highlight(t.Union(t.Panel(Requests, "clip-split"), t.Panel(Requests, "clip-duration"))));
            steps.Add(Click(t.Panel(Requests, "clip-post"), "Looks good. Post it!").Until(() => c.QuestPosted));

            // NOTE: The board's Close does nothing while the clipboard is still stamping — wait until it has slid away.
            steps.Add(Click(t.Panel(Requests, "board-close"), "Your quest is on the quest board now. Close the requests.")
                      .When(() => t.IsHidden(Requests, "clipboard-layer"))
                      .Until(() => c.IsShowing(Desk)));
            steps.Add(Click(t.Panel(Desk, "desk-close"), "Close the desk.").Until(() => c.NothingOpen));
        }

        // The quest board: slots, the posted note, then the starting party signs up.
        private static void AppendQuestBoard(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(ClickWorld(c.QuestBoard, "Posted quests hang on the Quest Board. Click it.", () => c.IsShowing(Board)));
            steps.Add(Say("Each quest takes a slot: 3 to start, plus one more for every guild rank.").Highlight(t.Panel(Board, "qboard-title")));
            steps.Add(Click(t.PanelFirst(Board, "quest-note--paper"), "That's your quest. Click the note."));
            steps.Add(Say("Its terms, time left, chance of success and who signed up. Nobody yet!").Highlight(t.Panel(Board, "note-lifted")));
            steps.Add(Click(t.Panel(Board, "note-back"), "Put it back."));
            steps.Add(Click(t.Panel(Board, "qboard-close"), "Close the board.")
                      .When(() => t.IsHidden(Board, "note-layer"))
                      .Until(() => c.NothingOpen));
            steps.Add(Do(c.SignUpStartingParty));
            steps.Add(Say(() => $"{c.StartingPartyNames} have applied! Applications are reviewed at the reception desk."));
        }

        // Applications: read them, approve, back out.
        private static void AppendApplications(List<TutorialStep> steps, TutorialContext c)
        {
            var t = c.Targets;
            steps.Add(ClickWorld(c.Desk, "Click the Reception Desk.", () => c.IsShowing(Desk)));
            steps.Add(Click(t.Panel(Desk, "desk-applications"), "The right half holds applications. Open them.")
                      .Until(() => c.IsShowing(Apps)));
            steps.Add(Click(t.PanelFirst(Apps, "app-row"), "Quests with applicants are listed here. Select yours."));
            steps.Add(Say("The quest and the terms you set.")
                      .Highlight(t.Union(t.Panel(Apps, "app-title"), t.Panel(Apps, "app-terms"))));
            steps.Add(Say("Who applied: class, rank and level. Decline anyone you'd rather not send.")
                      .Highlight(t.Panel(Apps, "app-applicants")));
            steps.Add(Say("Time left, and the chance of success with this party. Stronger parties do better.")
                      .Highlight(t.Panel(Apps, "app-time-chance")));
            steps.Add(Click(t.Panel(Apps, "app-approve"), "The party is full. Approve it to send them off!").Until(() => c.QuestDeparted));
            steps.Add(Click(t.Panel(Apps, "app-close"), "They're on their way! Close the applications.").Until(() => c.IsShowing(Desk)));
            steps.Add(Click(t.Panel(Desk, "desk-close"), "Close the desk.").Until(() => c.NothingOpen));
        }
    }
}