// The applications details pane: one scroll view with the quest summary and every applicant (each with Decline);
// Decline all and Approve stay below it.
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ApplicationDetailsView
    {
        private readonly ScrollView _content;
        private readonly OutlinedText _empty;
        private readonly OutlinedText _title;
        private readonly OutlinedText _meta;
        private readonly OutlinedText _terms;
        private readonly OutlinedText _split;
        private readonly OutlinedText _timeChance;
        private readonly OutlinedText _hint;
        private readonly VisualElement _applicantList;
        private readonly List<ApplicantRowView> _applicants = new();
        private readonly List<Button> _declineButtons = new();

        public Button DeclineAll { get; }
        public Button Approve { get; }

        // The pooled per-applicant Decline buttons, in list order (for setup and navigation).
        public IReadOnlyList<Button> DeclineButtons => _declineButtons;

        private ApplicationDetailsView(VisualElement root)
        {
            _content = root.Q<ScrollView>("app-content");
            _empty = root.Q<OutlinedText>("app-empty");
            _title = root.Q<OutlinedText>("app-title");
            _meta = root.Q<OutlinedText>("app-meta");
            _terms = root.Q<OutlinedText>("app-terms");
            _split = root.Q<OutlinedText>("app-split");
            _timeChance = root.Q<OutlinedText>("app-time-chance");
            _hint = root.Q<OutlinedText>("app-hint");
            _applicantList = root.Q<VisualElement>("app-applicants");
            DeclineAll = root.Q<Button>("app-decline-all");
            Approve = root.Q<Button>("app-approve");
        }

        private bool IsComplete =>
            _content != null && _empty != null && _title != null && _meta != null && _terms != null && _split != null &&
            _timeChance != null && _hint != null && _applicantList != null && DeclineAll != null && Approve != null;

        /// <param name="root"></param>
        /// <param name="maxApplicants">Pooled applicant rows — the largest party size the settings allow.</param>
        /// <param name="onDecline"></param>
        public static ApplicationDetailsView Bind(VisualElement root, int maxApplicants, Action<Adventurer> onDecline)
        {
            ApplicationDetailsView view = new(root);
            if (!view.IsComplete)
                return null;

            // NOTE: The whole pane scrolls (summary + applicants); a focused Decline scrolls its row into view.
            for (var i = 0; i < Math.Max(1, maxApplicants); i++)
            {
                ApplicantRowView row = new(onDecline);
                row.Decline.RegisterCallback<FocusEvent>(_ => view._content.ScrollTo(row.Root));
                view._applicantList.Add(row.Root);
                view._applicants.Add(row);
                view._declineButtons.Add(row.Decline);
            }
            return view;
        }

        public void Show(Quest quest, IReadOnlyList<Adventurer> applicants, float successChance, long nowMinutes)
        {
            var request = quest.Request;

            _title.Text = request.DisplayName;
            _meta.Text = QuestFormatter.Meta(request);
            _terms.Text = QuestFormatter.Terms(quest);
            _split.Text = QuestFormatter.GoldSplit(request, quest.Terms);
            _timeChance.Text = ApplicationFormatter.TimeAndChance(quest, successChance, applicants.Count > 0, nowMinutes);

            var hint = ApplicationFormatter.ApproveHint(quest);
            _hint.Text = hint;
            _hint.style.display = hint.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            for (var i = 0; i < _applicants.Count; i++)
            {
                if (i < applicants.Count)
                    _applicants[i].Show(applicants[i]);
                else
                    _applicants[i].Hide();
            }

            _content.style.display = DisplayStyle.Flex;
            _empty.style.display = DisplayStyle.None;
            DeclineAll.SetEnabled(applicants.Count > 0);
            Approve.SetEnabled(quest.State == QuestState.Ready);
        }

        public void ShowEmpty(string message)
        {
            _empty.Text = message;
            _content.style.display = DisplayStyle.None;
            _empty.style.display = DisplayStyle.Flex;
            _hint.style.display = DisplayStyle.None;

            foreach (var row in _applicants)
                row.Hide();

            DeclineAll.SetEnabled(false);
            Approve.SetEnabled(false);
        }
    }
}