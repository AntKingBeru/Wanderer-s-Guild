// Part 2 of the start tutorial (the party is away): build mode and its catalog, the guided rooms at their spots, skipping the
// bedrooms' wait (this once), and what beds mean for the adventurer limit.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static WanderersGuild.TutorialStepBuilder;

namespace WanderersGuild
{
    // NOTE: Time runs while building (the party walks out); explanations pause it. Rooms come from GameConfig › Tutorial ›
    // Placements in order: the first facility is named as "built at once", the first different one as "takes time". Their
    // construction is finished by the tutorial right after the last one is placed. The speeds moved to the controls part.
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
            steps.Add(Say("While they're out, let's grow the guild.").TimeRuns());
            steps.Add(Click(t.Hud("hud-build"), "Click the hammer to enter build mode.")
                      .BuildMode(TutorialBuildAccess.Open).Until(() => c.BuildModeActive).TimeRuns());
            steps.Add(Say(CatalogText(placements)).Highlight(t.BuildUi("build-catalog")).Controls(ControlGroup.Building)
                      .BuildMode(TutorialBuildAccess.Kept).TimeRuns());
            AppendPlacements(steps, c, placements);
            AppendSkipWait(steps, c, placements);

            steps.Add(Click(t.Hud("hud-build"), "Click the hammer to leave build mode.")
                      .BuildMode(TutorialBuildAccess.Open).Until(() => !c.BuildModeActive).TimeRuns());
            steps.Add(Say(() => $"Every bed raises your adventurer limit (now {c.Capacity}). Newcomers only join while there's " +
                                "room, and need a bed: give them one from the Lounge or by clicking a bedroom.")
                      .Highlight(t.Hud("adv-count-root")));
        }

        // NOTE: "Everything you can build. Some rooms, like the Hall, are built at once; others, like the Bedroom, take time."
        private static string CatalogText(IReadOnlyList<TutorialPlacement> placements)
        {
            var instant = placements[0].Facility;
            var timed = FirstOther(placements, instant);
            if (instant == null || timed == null)
                return "Everything you can build. Some rooms are built at once; others take time.";

            return $"Everything you can build. Some rooms, like the {instant.DisplayName}, are built at once; others, like " +
                   $"the {timed.DisplayName}, take time.";
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
            return (from placement in placements where placement.Facility != null && placement.Facility != other select placement.Facility).FirstOrDefault();
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