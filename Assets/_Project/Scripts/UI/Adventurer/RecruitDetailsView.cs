// The Recruits details pane: name, species · class, rank · level, stats, wealth, affinities, leaves-in, full hint, Decline/Accept.
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RecruitDetailsView
    {
        private readonly VisualElement _content;
        private readonly OutlinedText _empty;
        private readonly OutlinedText _name;
        private readonly OutlinedText _speciesClass;
        private readonly OutlinedText _rankLevel;
        private readonly OutlinedText _statsTop;
        private readonly OutlinedText _statsBottom;
        private readonly OutlinedText _wealth;
        private readonly OutlinedText _affinities;
        private readonly OutlinedText _leaves;
        private readonly OutlinedText _hint;

        public Button Decline { get; }
        public Button Accept { get; }

        private RecruitDetailsView(VisualElement root)
        {
            _content = root.Q<VisualElement>("rec-content");
            _empty = root.Q<OutlinedText>("rec-empty");
            _name = root.Q<OutlinedText>("rec-name");
            _speciesClass = root.Q<OutlinedText>("rec-species-class");
            _rankLevel = root.Q<OutlinedText>("rec-rank-level");
            _statsTop = root.Q<OutlinedText>("rec-stats-top");
            _statsBottom = root.Q<OutlinedText>("rec-stats-bottom");
            _wealth = root.Q<OutlinedText>("rec-wealth");
            _affinities = root.Q<OutlinedText>("rec-affinities");
            _leaves = root.Q<OutlinedText>("rec-leaves");
            _hint = root.Q<OutlinedText>("rec-hint");
            Decline = root.Q<Button>("rec-decline");
            Accept = root.Q<Button>("rec-accept");
        }

        private bool IsComplete =>
            _content != null && _empty != null && _name != null && _speciesClass != null && _rankLevel != null &&
            _statsTop != null && _statsBottom != null && _wealth != null && _affinities != null && _leaves != null &&
            _hint != null && Decline != null && Accept != null;

        public static RecruitDetailsView Bind(VisualElement root)
        {
            RecruitDetailsView view = new(root);
            return view.IsComplete ? view : null;
        }

        public void Show(Applicant applicant, long nowMinutes, int members, int capacity)
        {
            var a = applicant.Adventurer;
            _name.Text = a.Name;
            _speciesClass.Text = AdventurerFormatter.SpeciesAndClass(a);
            _rankLevel.Text = AdventurerFormatter.RankAndLevel(a);
            _statsTop.Text = AdventurerFormatter.StatsTop(a.Stats);
            _statsBottom.Text = AdventurerFormatter.StatsBottom(a.Stats);
            _wealth.Text = AdventurerFormatter.Wealth(a);
            _affinities.Text = AdventurerFormatter.Affinities(a);
            _leaves.Text = RecruitFormatter.LeavesIn(applicant, nowMinutes);

            var hint = RecruitFormatter.FullHint(members, capacity);
            _hint.Text = hint;
            _hint.style.display = hint.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            _content.style.display = DisplayStyle.Flex;
            _empty.style.display = DisplayStyle.None;
            Decline.SetEnabled(true);
            Accept.SetEnabled(hint.Length == 0);
        }

        public void ShowEmpty(string message)
        {
            _empty.Text = message;
            _content.style.display = DisplayStyle.None;
            _empty.style.display = DisplayStyle.Flex;
            _hint.style.display = DisplayStyle.None;
            Decline.SetEnabled(false);
            Accept.SetEnabled(false);
        }
    }
}