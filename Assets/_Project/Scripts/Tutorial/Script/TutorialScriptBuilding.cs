// Part 2 of the start tutorial (the party is away): build mode, the hall and bedroom cards, the guided rooms at their spots,
// skipping the bedrooms' wait (this once), the speed buttons, and what beds mean for the adventurer limit.
using System.Collections.Generic;
using UnityEngine;
using static WanderersGuild.TutorialStepBuilder;

namespace WanderersGuild
{
    // NOTE: Time runs while building (the party walks out); explanations pause it. Rooms come from GameConfig › Tutorial ›
    // Placements in order: the first facility is shown as "built at once", the first different one as "takes time". Their
    // construction is finished by the tutorial right after the last one is placed.
    public static class TutorialScriptBuilding
    {
        public static void Append(List<TutorialStep> steps, TutorialContext c)
        {
            var placements = c.Placements;
            if (placements.Count == 0)
            {
                Debug.LogWarning("[Tutorial] GameConfig › Tutorial › Placements is empty — the building part is skipped.");
                return;
            }

            var t = c.Targets;
            steps.Add(Say("They're on their way! While they're out, let's grow the guild. Your adventurers still sleep outside.").TimeRuns());
            steps.Add(Click(t.Hud("hud-build"), "Click the hammer to enter build mode.")
                      .BuildMode(TutorialBuildAccess.Open).Until(() => c.BuildModeActive).TimeRuns());
            steps.Add(Say("Everything you can build. Locked rooms show what they need.")
                      .Highlight(t.BuildUi("build-catalog")).BuildMode(TutorialBuildAccess.Kept).TimeRuns());
            AppendCards(steps, c, placements);
            AppendPlacements(steps, c, placements);
            AppendSkipWait(steps, c, placements);

            steps.Add(Click(t.Hud("hud-build"), "Click the hammer to leave build mode.")
                      .BuildMode(TutorialBuildAccess.Open).Until(() => !c.BuildModeActive).TimeRuns());
            steps.Add(Say("These set the speed of time: pause, normal, fast and very fast. Speed up whenever you're waiting for " +
                          "something, like a room being built.")
                      .Highlight(t.HudFirst("hud-speeds")));
            steps.Add(Say(() => $"Bedrooms add beds, and every bed raises your adventurer limit (now {c.Capacity}). " +
                                "New adventurers only join while there's room.")
                      .Highlight(t.Hud("adv-count-root")));
        }

        // NOTE: The hall builds at once (0 days); the bedroom takes time.
        private static void AppendCards(List<TutorialStep> steps, TutorialContext c, IReadOnlyList<TutorialPlacement> placements)
        {
            var t = c.Targets;
            var instant = placements[0].Facility;
            var timed = FirstOther(placements, instant);

            if (instant != null)
                steps.Add(Say($"{instant.DisplayName}s connect your rooms. Some rooms, like this one, are built at once.")
                          .Highlight(t.BuildCard(instant)).BuildMode(TutorialBuildAccess.Kept).TimeRuns());
            if (timed != null)
                steps.Add(Say($"Others, like the {timed.DisplayName}, take time to build.")
                          .Highlight(t.BuildCard(timed)).BuildMode(TutorialBuildAccess.Kept).TimeRuns());
        }

        private static void AppendPlacements(List<TutorialStep> steps, TutorialContext c, IReadOnlyList<TutorialPlacement> placements)
        {
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                if (placement.Facility == null)
                {
                    Debug.LogWarning($"[Tutorial] Placement {i} has no facility; skipped.");
                    continue;
                }

                steps.Add(Place(c.Targets.Placement(placement), PlaceText(placements, i), () => c.IsPlaced(placement)).TimeRuns());
            }
        }

        // NOTE: Time is paused while this is read (nothing finishes early); build mode stays kept so the player watches the
        // scaffolds become rooms. The finish goes through the normal completion (lodging, capacity, doors, nav grid).
        private static void AppendSkipWait(List<TutorialStep> steps, TutorialContext c, IReadOnlyList<TutorialPlacement> placements)
        {
            steps.Add(Say("Bedrooms usually take a while to build. Just this once, we'll skip the wait so you can see the result.")
                      .BuildMode(TutorialBuildAccess.Kept));
            steps.Add(Do(() => c.FinishPlacementsNow(placements)));
        }

        // NOTE: The first room of each kind gets the full instruction; the rest a short one.
        private static string PlaceText(IReadOnlyList<TutorialPlacement> placements, int index)
        {
            var name = placements[index].Facility.DisplayName;
            if (FirstIndexOf(placements, placements[index].Facility) != index)
                return $"Build another {name} on the highlighted spot.";

            return index == 0
                ? $"Pick the {name} card, then click the highlighted spot and confirm."
                : $"Now the {name}s, one beside each hall. Pick the {name} card and build it on the spot.";
        }

        private static FacilityData FirstOther(IReadOnlyList<TutorialPlacement> placements, FacilityData other)
        {
            foreach (var placement in placements)
            {
                if (placement.Facility != null && placement.Facility != other)
                    return placement.Facility;
            }
            return null;
        }

        private static int FirstIndexOf(IReadOnlyList<TutorialPlacement> placements, FacilityData facility)
        {
            for (var i = 0; i < placements.Count; i++)
            {
                if (placements[i].Facility == facility)
                    return i;
            }
            return -1;
        }
    }
}