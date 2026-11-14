// The guided start tutorial, in order: request → quest, building while the party is away, then the results and the roster.
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: The results part ends with the step that completes the tutorial — the only thing that does.
    public static class TutorialScript
    {
        public static List<TutorialStep> Build(TutorialContext context)
        {
            List<TutorialStep> steps = new();
            TutorialScriptRequests.Append(steps, context);
            TutorialScriptBuilding.Append(steps, context);
            TutorialScriptResults.Append(steps, context);
            return steps;
        }
    }
}