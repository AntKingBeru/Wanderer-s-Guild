using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class QuitConfirmationPanel : MenuPanel
    {
        private const string NoName = "quit-no";
        private const string YesName = "quit-yes";

        private readonly List<Button> _buttons = new();
        private readonly IMenuCommand _quitCommand = new QuitGameCommand();
        private Button _no;
        private Button _yes;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        
        protected override Focusable DefaultFocus => _no;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _no = documentRoot.Q<Button>(NoName);
            _yes = documentRoot.Q<Button>(YesName);

            _buttons.Clear();
            if (_no == null || _yes == null)
                return
                false;

            _buttons.Add(_no);
            _buttons.Add(_yes);
            return true;
        }

        protected override void OnInitialize()
        {
            _no.clicked += OnNo;
            _yes.clicked += OnYes;

            // Left/Right (and Tab) move between No and Yes. Esc is handled by the base (closes = No).
            AddPanelManipulator(new MenuNavigation(_buttons, () => IsInteractable, null, NavigationAxis.Horizontal));
        }

        private void OnNo()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        private void OnYes()
        {
            if (IsInteractable && _quitCommand.CanExecute)
                _quitCommand.Execute();
        }
    }
}