// Part 0 of the start tutorial (also shown alone after a skip): the controls, one card each — getting around (try it: move
// and zoom), time, and menus.
using System.Collections.Generic;
using static WanderersGuild.TutorialStepBuilder;

namespace WanderersGuild
{
    // NOTE: Time stays paused (the camera uses real time, so practising still works). The cards list GameConfig › Controls
    // entries by their Group.
    public static class TutorialScriptControls
    {
        public static void Append(List<TutorialStep> steps, TutorialContext c)
        {
            var practice = c.CameraPractice;
            steps.Add(Say("Welcome, Guild Master! First, getting around. Move and zoom the camera to continue.")
                .Controls(ControlGroup.Camera).OnEnter(practice.Begin).Until(() => practice.IsDone));
            steps.Add(Say("Time: pause it, or speed it up whenever you're waiting for something.")
                .Controls(ControlGroup.Time).Highlight(c.Targets.HudFirst("hud-speeds")).OnEnter(practice.End));
            steps.Add(Say("Menus and screens. Every control is also listed in Options › Controls.")
                .Controls(ControlGroup.Menus));
        }

        // NOTE: After a skip — the same cards, then the script simply ends (the tutorial is already done).
        public static List<TutorialStep> BuildAlone(TutorialContext context)
        {
            List<TutorialStep> steps = new();
            Append(steps, context);
            return steps;
        }
    }
}