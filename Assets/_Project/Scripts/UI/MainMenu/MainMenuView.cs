using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuView : MonoBehaviour
    {
        private const string RootName = "menu-root";
        private const string LogoName = "menu-logo";
        private const string ColumnName = "menu-column";

        private readonly Dictionary<MenuAction, Button> _buttons = new();
        private readonly List<Button> _buttonsInOrder = new();
        private readonly List<VisualElement> _columnItems = new();

        public bool IsBound { get; private set; }
        public VisualElement Root { get; private set; }
        public VisualElement Logo { get; private set; }

        //The panel's top element. Navigation events land here when nothing is focused.
        public VisualElement PanelRoot => Root?.panel?.visualTree;

        //Buttons in on-screen order (top to bottom). Each button's userData is its MenuAction.
        public IReadOnlyList<Button> ButtonsInOrder => _buttonsInOrder;

        //Every column child (buttons and dividers) in on-screen order. Used by the entrance stagger.
        public IReadOnlyList<VisualElement> ColumnItems => _columnItems;

        private void OnEnable() => Bind();

        //Enables or blocks hover, clicks and focus on every button (used to gate input during the entrance).
        public void SetButtonsInteractable(bool interactable)
        {
            foreach (var button in _buttonsInOrder)
            {
                button.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
                button.focusable = interactable;
            }
        }

        private void Bind()
        {
            _buttons.Clear();
            _buttonsInOrder.Clear();
            _columnItems.Clear();

            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            Root = documentRoot?.Q<VisualElement>(RootName);
            Logo = documentRoot?.Q<VisualElement>(LogoName);
            var column = documentRoot?.Q<VisualElement>(ColumnName);

            IsBound = Root != null && Logo != null && column != null;
            if (!IsBound)
            {
                Debug.LogError("[MainMenuView] Missing menu-root, menu-logo or menu-column in the UXML.", this);
                return;
            }

            if (column != null)
            {
                foreach (var child in column.Children())
                    _columnItems.Add(child);

                foreach (var action in (MenuAction[])Enum.GetValues(typeof(MenuAction)))
                {
                    var button = column.Q<Button>(ElementName(action));
                    if (button == null)
                    {
                        Debug.LogError($"[MainMenuView] No button named '{ElementName(action)}' for {action}.", this);
                        continue;
                    }

                    // NOTE: Drops the default theme's button styles (hover tint, radius), so only our USS frames apply.
                    button.RemoveFromClassList(Button.ussClassName);
                    button.userData = action;
                    _buttons[action] = button;
                }
            }

            foreach (var item in _columnItems)
            {
                if (item is Button { userData: MenuAction } button)
                    _buttonsInOrder.Add(button);
            }
        }

        private static string ElementName(MenuAction action) => action switch
        {
            MenuAction.Continue => "menu-button-continue",
            MenuAction.NewGame => "menu-button-new-game",
            MenuAction.LoadGame => "menu-button-load-game",
            MenuAction.Options => "menu-button-options",
            MenuAction.Credits => "menu-button-credits",
            MenuAction.QuitGame => "menu-button-quit-game",
            _ => string.Empty
        };
    }
}