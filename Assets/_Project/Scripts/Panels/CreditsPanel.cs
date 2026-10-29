using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class CreditsPanel : MenuPanel
    {
        private const string ScrollName = "credits-scroll";
        private const string BackName = "credits-back";
        private const string HeadingClass = "credits-heading";
        private const string NameClass = "credits-name";
        private const string SpacerClass = "credits-spacer";

        [SerializeField] private CreditsData data;

        private readonly List<Button> _buttons = new();
        private ScrollView _scroll;
        private Button _back;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override Focusable DefaultFocus => _back;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _scroll = documentRoot.Q<ScrollView>(ScrollName);
            _back = documentRoot.Q<Button>(BackName);

            _buttons.Clear();
            if (_back != null)
                _buttons.Add(_back);
            return _scroll != null && _back != null;
        }

        protected override void OnInitialize()
        {
            var settings = GameConfig.Instance.Credits;
            _scroll.mouseWheelScrollSize = settings.WheelScrollStep;
            _back.clicked += OnBack;

            AddPanelManipulator(new ScrollKeyNavigation(_scroll, () => IsInteractable, settings.KeyScrollStep));
            BuildContent();
        }

        // NOTE: Every open starts at the top of the list.
        protected override void OnOpening() => _scroll.scrollOffset = Vector2.zero;

        private void OnBack()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        private void BuildContent()
        {
            _scroll.Clear();

            if (!data || data.Sections.Count == 0)
            {
                AddLine("Credits coming soon.", NameClass);
                return;
            }

            foreach (var section in data.Sections)
            {
                if (!string.IsNullOrEmpty(section.Heading))
                    AddLine(section.Heading, HeadingClass);

                foreach (var person in section.Names)
                    AddLine(person, NameClass);

                VisualElement spacer = new();
                spacer.AddToClassList(SpacerClass);
                _scroll.Add(spacer);
            }
        }

        private void AddLine(string text, string ussClass)
        {
            OutlinedText line = new() { Text = text };
            line.AddToClassList(ussClass);
            _scroll.Add(line);
        }
    }
}