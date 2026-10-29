// The request board's details pane: title, category/source, rank/party, reward, time left, objective, board-full note, Accept/Dismiss.
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RequestDetailsView
    {
        private const string ContentName = "detail-content";
        private const string EmptyName = "detail-empty";
        private const string UrgentClass = "detail-time--urgent";

        private readonly VisualElement _content;
        private readonly OutlinedText _empty;
        private readonly OutlinedText _title;
        private readonly OutlinedText _categorySource;
        private readonly OutlinedText _rankParty;
        private readonly OutlinedText _reward;
        private readonly OutlinedText _time;
        private readonly OutlinedText _boardFull;
        private readonly OutlinedText _objective;

        public Button Accept { get; }
        public Button Dismiss { get; }

        private RequestDetailsView(VisualElement root)
        {
            _content = root.Q<VisualElement>(ContentName);
            _empty = root.Q<OutlinedText>(EmptyName);
            _title = root.Q<OutlinedText>("detail-title");
            _categorySource = root.Q<OutlinedText>("detail-category");
            _rankParty = root.Q<OutlinedText>("detail-rank");
            _reward = root.Q<OutlinedText>("detail-reward");
            _time = root.Q<OutlinedText>("detail-time");
            _boardFull = root.Q<OutlinedText>("detail-board");
            _objective = root.Q<OutlinedText>("detail-objective");
            Accept = root.Q<Button>("detail-accept");
            Dismiss = root.Q<Button>("detail-dismiss");
        }

        private bool IsComplete =>
            _content != null && _empty != null && _title != null && _categorySource != null && _rankParty != null &&
            _reward != null && _time != null && _boardFull != null && _objective != null && Accept != null && Dismiss != null;

        public static RequestDetailsView Bind(VisualElement root)
        {
            RequestDetailsView view = new(root);
            return view.IsComplete ? view : null;
        }

        /// <param name="request"></param>
        /// <param name="nowMinutes"></param>
        /// <param name="postedQuests">Quests on the quest board.</param>
        /// <param name="openSlots">Its open slots; 0 = no quest board (no QuestManager) — Accept disabled, no note.</param>
        public void Show(ActiveRequest request, long nowMinutes, int postedQuests, int openSlots)
        {
            var data = request.Data;
            var minutesLeft = request.MinutesLeft(nowMinutes);
            var hasBoard = openSlots > 0;
            var boardFull = hasBoard && postedQuests >= openSlots;

            _title.Text = data.DisplayName;
            _categorySource.Text = RequestFormatter.CategoryAndSource(data);
            _rankParty.Text = RequestFormatter.RankAndParty(data);
            _reward.Text = $"Reward: {RequestFormatter.Reward(data.Reward)}";
            _time.Text = $"Expires in {RequestFormatter.TimeLeft(minutesLeft)}";
            _time.EnableInClassList(UrgentClass, RequestFormatter.IsLastDay(minutesLeft));
            _objective.Text = data.Objective;

            _boardFull.Text = $"Quest board full ({postedQuests}/{openSlots})";
            _boardFull.style.display = boardFull ? DisplayStyle.Flex : DisplayStyle.None;

            _content.style.display = DisplayStyle.Flex;
            _empty.style.display = DisplayStyle.None;
            Accept.SetEnabled(hasBoard && !boardFull);
            Dismiss.SetEnabled(true);
        }

        public void ShowEmpty(string message)
        {
            _empty.Text = message;
            _content.style.display = DisplayStyle.None;
            _empty.style.display = DisplayStyle.Flex;
            _boardFull.style.display = DisplayStyle.None;
            Accept.SetEnabled(false);
            Dismiss.SetEnabled(false);
        }
    }
}