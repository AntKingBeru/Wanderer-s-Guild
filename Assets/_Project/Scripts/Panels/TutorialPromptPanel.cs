// The new-guild prompt: play the guided tutorial, or skip it (skipping builds the tutorial's end state). Esc does nothing.
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class TutorialPromptPanel : MenuPanel
    {
        private const string PlayName = "tutorial-play";
        private const string SkipName = "tutorial-skip";

        private readonly List<Button> _buttons = new();
        private Button _play;
        private Button _skip;

        protected override IReadOnlyList<Button> Buttons => _buttons;

        protected override Focusable DefaultFocus => _play;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _skip = documentRoot.Q<Button>(SkipName);
            _play = documentRoot.Q<Button>(PlayName);

            _buttons.Clear();
            if (_skip == null || _play == null)
                return false;

            _buttons.Add(_skip);
            _buttons.Add(_play);
            return true;
        }

        protected override void OnInitialize()
        {
            _play.clicked += OnPlay;
            _skip.clicked += OnSkip;
            AddPanelManipulator(new MenuNavigation(_buttons, () => IsInteractable, null, NavigationAxis.Horizontal));
        }

        // NOTE: The choice can't be dodged — the tutorial holds the game back until it's made.
        protected override void OnCancel() { }

        private void OnPlay()
        {
            if (!IsInteractable || !TutorialManager.HasInstance)
                return;

            TutorialManager.Instance.BeginGuided();
            Host.Close(Id);
        }

        // NOTE: The start state is built while the prompt is still up (time paused) — nothing needs time to pass.
        private void OnSkip()
        {
            if (!IsInteractable || !TutorialManager.HasInstance)
                return;

            TutorialManager.Instance.Skip();
            Host.Close(Id);
        }
    }
}