// The roster's details pane: name, species · class, rank · level, XP and rank-progress bars, stats, wealth, affinities, status,
// promotion hint and the Promote button.
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class AdventurerDetailsView
    {
        private readonly VisualElement _content;
        private readonly OutlinedText _empty;
        private readonly OutlinedText _name;
        private readonly OutlinedText _speciesClass;
        private readonly OutlinedText _rankLevel;
        private readonly OutlinedText _experience;
        private readonly OutlinedText _rankProgress;
        private readonly OutlinedText _statsTop;
        private readonly OutlinedText _statsBottom;
        private readonly OutlinedText _wealth;
        private readonly OutlinedText _affinities;
        private readonly OutlinedText _status;
        private readonly OutlinedText _hint;
        private readonly ExperienceBarView _experienceBar;
        private readonly ExperienceBarView _rankBar;

        public Button Promote { get; }

        private AdventurerDetailsView(VisualElement root, ExperienceBarView experienceBar, ExperienceBarView rankBar)
        {
            _content = root.Q<VisualElement>("adv-content");
            _empty = root.Q<OutlinedText>("adv-empty");
            _name = root.Q<OutlinedText>("adv-name");
            _speciesClass = root.Q<OutlinedText>("adv-species-class");
            _rankLevel = root.Q<OutlinedText>("adv-rank-level");
            _experience = root.Q<OutlinedText>("adv-xp-text");
            _rankProgress = root.Q<OutlinedText>("adv-rankp-text");
            _statsTop = root.Q<OutlinedText>("adv-stats-top");
            _statsBottom = root.Q<OutlinedText>("adv-stats-bottom");
            _wealth = root.Q<OutlinedText>("adv-wealth");
            _affinities = root.Q<OutlinedText>("adv-affinities");
            _status = root.Q<OutlinedText>("adv-status");
            _hint = root.Q<OutlinedText>("adv-promote-hint");
            Promote = root.Q<Button>("adv-promote");
            _experienceBar = experienceBar;
            _rankBar = rankBar;
        }

        private bool IsComplete =>
            _content != null && _empty != null && _name != null && _speciesClass != null && _rankLevel != null &&
            _experience != null && _rankProgress != null && _statsTop != null && _statsBottom != null && _wealth != null &&
            _affinities != null && _status != null && _hint != null && Promote != null;

        public static AdventurerDetailsView Bind(VisualElement root)
        {
            var xpTrack = root.Q<VisualElement>("adv-xp-track");
            var xpFill = root.Q<VisualElement>("adv-xp-fill");
            var rankTrack = root.Q<VisualElement>("adv-rankp-track");
            var rankFill = root.Q<VisualElement>("adv-rankp-fill");
            if (xpTrack == null || xpFill == null || rankTrack == null || rankFill == null)
                return null;

            AdventurerDetailsView view = new(root, new ExperienceBarView(xpTrack, xpFill), new ExperienceBarView(rankTrack, rankFill));
            return view.IsComplete ? view : null;
        }

        public void Show(Adventurer adventurer, ExperienceTable table, PromotionCheck check, PromotionSettings promotion, long nowMinutes)
        {
            var stats = adventurer.Stats;

            _name.Text = adventurer.Name;
            _speciesClass.Text = AdventurerFormatter.SpeciesAndClass(adventurer);
            _rankLevel.Text = AdventurerFormatter.RankAndLevel(adventurer);
            _experience.Text = AdventurerFormatter.Experience(adventurer, table);
            _experienceBar.SetProgress(adventurer.LevelProgress);
            _rankProgress.Text = AdventurerFormatter.RankProgress(adventurer, promotion);
            _rankBar.SetProgress(AdventurerFormatter.RankProgressFill(adventurer, promotion));
            _statsTop.Text = AdventurerFormatter.StatsTop(stats);
            _statsBottom.Text = AdventurerFormatter.StatsBottom(stats);
            _wealth.Text = AdventurerFormatter.Wealth(adventurer);
            _affinities.Text = AdventurerFormatter.Affinities(adventurer);
            _status.Text = AdventurerFormatter.Status(adventurer, nowMinutes);
            _hint.Text = AdventurerFormatter.PromotionHint(check);

            _content.style.display = DisplayStyle.Flex;
            _empty.style.display = DisplayStyle.None;
            Promote.SetEnabled(check.CanPromote);
        }

        public void ShowEmpty(string message)
        {
            _empty.Text = message;
            _content.style.display = DisplayStyle.None;
            _empty.style.display = DisplayStyle.Flex;
            Promote.SetEnabled(false);
        }
    }
}