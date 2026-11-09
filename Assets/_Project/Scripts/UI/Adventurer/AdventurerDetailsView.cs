// The roster's details pane: name, species · class, rank · level, XP and rank-progress bars, stats (with gear), wealth,
// affinities, gear, status, party, room, promotion / advancement hint; Promote (or Advance), Disband and Move buttons.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class AdventurerDetailsView
    {
        private readonly ScrollView _content;
        private readonly OutlinedText _empty;
        private readonly OutlinedText _name;
        private readonly IconSlot _classIcon;
        private readonly OutlinedText _speciesClass;
        private readonly OutlinedText _rankLevel;
        private readonly OutlinedText _experience;
        private readonly OutlinedText _rankProgress;
        private readonly OutlinedText _statsTop;
        private readonly OutlinedText _statsBottom;
        private readonly OutlinedText _wealth;
        private readonly OutlinedText _affinities;
        private readonly OutlinedText _gear;
        private readonly OutlinedText _status;
        private readonly OutlinedText _party;
        private readonly OutlinedText _room;
        private readonly OutlinedText _hint;
        private readonly OutlinedText _promoteLabel;
        private readonly ExperienceBarView _experienceBar;
        private readonly ExperienceBarView _rankBar;
        private int _shownId = -1;

        public Button Promote { get; }
        public Button Disband { get; }
        public Button Move { get; }

        // True when the Promote button currently means "Advance" (the adventurer is at their class cap).
        public bool IsAdvanceMode { get; private set; }

        private AdventurerDetailsView(VisualElement root, ExperienceBarView experienceBar, ExperienceBarView rankBar)
        {
            _content = root.Q<ScrollView>("adv-content");
            _empty = root.Q<OutlinedText>("adv-empty");
            _name = root.Q<OutlinedText>("adv-name");
            _classIcon = IconSlot.Bind(root, "adv-class-icon");
            _speciesClass = root.Q<OutlinedText>("adv-species-class");
            _rankLevel = root.Q<OutlinedText>("adv-rank-level");
            _experience = root.Q<OutlinedText>("adv-xp-text");
            _rankProgress = root.Q<OutlinedText>("adv-rankp-text");
            _statsTop = root.Q<OutlinedText>("adv-stats-top");
            _statsBottom = root.Q<OutlinedText>("adv-stats-bottom");
            _wealth = root.Q<OutlinedText>("adv-wealth");
            _affinities = root.Q<OutlinedText>("adv-affinities");
            _gear = root.Q<OutlinedText>("adv-gear");
            _status = root.Q<OutlinedText>("adv-status");
            _party = root.Q<OutlinedText>("adv-party");
            _room = root.Q<OutlinedText>("adv-room");
            _hint = root.Q<OutlinedText>("adv-promote-hint");
            Promote = root.Q<Button>("adv-promote");
            Disband = root.Q<Button>("adv-disband");
            Move = root.Q<Button>("adv-move");
            _promoteLabel = Promote?.Q<OutlinedText>();
            _experienceBar = experienceBar;
            _rankBar = rankBar;
        }

        private bool IsComplete =>
            _content != null && _empty != null && _name != null && _classIcon != null && _speciesClass != null && _rankLevel != null &&
            _experience != null && _rankProgress != null && _statsTop != null && _statsBottom != null && _wealth != null &&
            _affinities != null && _gear != null && _status != null && _party != null && _room != null && _hint != null &&
            Promote != null && _promoteLabel != null && Disband != null && Move != null;

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

        /// <param name="adventurer"></param>
        /// <param name="table"></param>
        /// <param name="check"></param>
        /// <param name="promotion"></param>
        /// <param name="nowMinutes"></param>
        /// <param name="partyLine">"Party: The Iron Wolves (3) · Teamwork +4%", or empty (hidden; Disband disabled).</param>
        /// <param name="roomLine">"Room: Bedroom 2" / "Room: Lodging outside", or empty (hidden).</param>
        /// <param name="canMove">Another bedroom has a free bed (Move enabled).</param>
        /// <param name="advancement">At the class cap the button becomes "Advance" with this check's hint.</param>
        // NOTE: Stats include gear (Adventurer.Stats). A different adventurer starts at the top of the pane; refreshing the
        // same one keeps the reader's scroll position.
        public void Show(Adventurer adventurer, ExperienceTable table, PromotionCheck check, PromotionSettings promotion,
                         long nowMinutes, string partyLine, string roomLine, bool canMove, AdvancementCheck advancement)
        {
            var stats = adventurer.Stats;

            _name.Text = adventurer.Name;
            _speciesClass.Text = AdventurerFormatter.SpeciesAndClass(adventurer);
            _classIcon.Set(adventurer.Class.Icon, adventurer.Class.DisplayName);
            _rankLevel.Text = AdventurerFormatter.RankAndLevel(adventurer);
            _experience.Text = AdventurerFormatter.Experience(adventurer, table);
            _experienceBar.SetProgress(adventurer.LevelProgress);
            _rankProgress.Text = AdventurerFormatter.RankProgress(adventurer, promotion);
            _rankBar.SetProgress(AdventurerFormatter.RankProgressFill(adventurer, promotion));
            _statsTop.Text = AdventurerFormatter.StatsTop(stats);
            _statsBottom.Text = AdventurerFormatter.StatsBottom(stats);
            _wealth.Text = AdventurerFormatter.Wealth(adventurer);
            _affinities.Text = AdventurerFormatter.Affinities(adventurer);
            _gear.Text = AdventurerFormatter.Gear(adventurer);
            _status.Text = AdventurerFormatter.Status(adventurer, nowMinutes);
            _party.Text = partyLine;
            _party.style.display = string.IsNullOrEmpty(partyLine) ? DisplayStyle.None : DisplayStyle.Flex;
            _room.Text = roomLine;
            _room.style.display = string.IsNullOrEmpty(roomLine) ? DisplayStyle.None : DisplayStyle.Flex;

            IsAdvanceMode = advancement.IsAtClassCap;
            _promoteLabel.Text = IsAdvanceMode ? "Advance" : "Promote";
            _hint.Text = IsAdvanceMode ? AdvancementFormatter.Hint(advancement) : AdventurerFormatter.PromotionHint(check);

            if (adventurer.Id != _shownId)
            {
                _shownId = adventurer.Id;
                _content.scrollOffset = Vector2.zero;
            }

            _content.style.display = DisplayStyle.Flex;
            _empty.style.display = DisplayStyle.None;
            Promote.SetEnabled(IsAdvanceMode ? advancement.CanAdvance : check.CanPromote);
            Disband.SetEnabled(!string.IsNullOrEmpty(partyLine));
            Move.SetEnabled(canMove);
        }

        public void ShowEmpty(string message)
        {
            _shownId = -1;
            IsAdvanceMode = false;
            _promoteLabel.Text = "Promote";
            _empty.Text = message;
            _content.style.display = DisplayStyle.None;
            _empty.style.display = DisplayStyle.Flex;
            Promote.SetEnabled(false);
            Disband.SetEnabled(false);
            Move.SetEnabled(false);
        }
    }
}