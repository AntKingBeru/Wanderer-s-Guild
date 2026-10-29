// The lifted note: a large readable copy of a pinned quest over the dimmed board, with Back and Cancel; scales up and back.
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class QuestNoteDetailView
    {
        private const string StampedClass = "note-lifted--stamped";
        private const string ReadyClass = "note-lifted--ready";
        private const string UrgentClass = "note-time--urgent";
        private const string NoMembersText = "No sign-ups yet.";

        private readonly VisualElement _layer;
        private readonly VisualElement _note;
        private readonly VisualElement _badge;
        private readonly OutlinedText _badgeLabel;
        private readonly OutlinedText _title;
        private readonly OutlinedText _objective;
        private readonly OutlinedText _meta;
        private readonly OutlinedText _terms;
        private readonly OutlinedText _split;
        private readonly OutlinedText _time;
        private readonly OutlinedText _chance;
        private readonly OutlinedText _stamp;
        private readonly VisualElement _members;
        private readonly List<OutlinedText> _memberLines = new();
        private readonly GameObject _linkTarget;
        private Sequence _sequence;
        private float _shown;

        public Button Back { get; }
        public Button Cancel { get; }
        public bool IsOpen { get; private set; }

        // True while lifting or putting back — input is ignored then.
        public bool IsBusy { get; private set; }

        private QuestNoteDetailView(VisualElement root, GameObject linkTarget)
        {
            _linkTarget = linkTarget;
            _layer = root.Q<VisualElement>("note-layer");
            _note = root.Q<VisualElement>("note-lifted");
            _badge = root.Q<VisualElement>("note-badge");
            _badgeLabel = root.Q<OutlinedText>("note-badge-label");
            _title = root.Q<OutlinedText>("note-title");
            _objective = root.Q<OutlinedText>("note-objective");
            _meta = root.Q<OutlinedText>("note-meta");
            _terms = root.Q<OutlinedText>("note-terms");
            _split = root.Q<OutlinedText>("note-split");
            _time = root.Q<OutlinedText>("note-time");
            _chance = root.Q<OutlinedText>("note-chance");
            _stamp = root.Q<OutlinedText>("note-stamp");
            _members = root.Q<VisualElement>("note-members");
            Back = root.Q<Button>("note-back");
            Cancel = root.Q<Button>("note-cancel");
        }

        private bool IsComplete =>
            _layer != null && _note != null && _badge != null && _badgeLabel != null && _title != null && _objective != null &&
            _meta != null && _terms != null && _split != null && _time != null && _chance != null && _stamp != null &&
            _members != null && Back != null && Cancel != null;

        public static QuestNoteDetailView Bind(VisualElement root, GameObject linkTarget)
        {
            QuestNoteDetailView view = new(root, linkTarget);
            if (!view.IsComplete)
                return null;

            view.HideImmediate();
            return view;
        }

        /// <param name="quest"></param>
        /// <param name="members">Current members (from QuestManager.MembersOf).</param>
        /// <param name="successChance"></param>
        /// <param name="nowMinutes"></param>
        /// <param name="rankColor"></param>
        public void Show(Quest quest, IReadOnlyList<Adventurer> members, float successChance, long nowMinutes, Color rankColor)
        {
            var request = quest.Request;
            var stamp = QuestFormatter.Stamp(quest);

            _badge.style.backgroundColor = rankColor;
            _badgeLabel.Text = RankFormatter.BadgeLabel(request.RecommendedRank);
            _title.Text = request.DisplayName;
            _objective.Text = request.Objective;
            _meta.Text = QuestFormatter.Meta(request);
            _terms.Text = QuestFormatter.Terms(quest);
            _split.Text = QuestFormatter.GoldSplit(request, quest.Terms);
            _time.Text = QuestFormatter.TimeLine(quest, nowMinutes);
            _time.EnableInClassList(UrgentClass, QuestFormatter.IsUrgent(quest, nowMinutes));
            _chance.Text = QuestFormatter.Chance(successChance, members.Count > 0);
            _stamp.Text = stamp;
            ShowMembers(members);

            _note.EnableInClassList(StampedClass, stamp.Length > 0);
            _note.EnableInClassList(ReadyClass, quest.State == QuestState.Ready);
            Cancel.SetEnabled(quest.IsBeforeDeparture);
        }

        public void HideImmediate()
        {
            Kill();
            IsOpen = false;
            IsBusy = false;
            Apply(0f);
            _layer.style.display = DisplayStyle.None;
        }

        public void Open(QuestPanelSettings settings, Action onOpened)
        {
            Kill();
            IsOpen = true;
            IsBusy = true;
            _layer.style.display = DisplayStyle.Flex;

            _sequence = DOTween.Sequence()
                .Append(DOVirtual.Float(_shown, 1f, settings.LiftDuration, Apply).SetEase(Ease.OutBack))
                .OnComplete(() => { IsBusy = false; onOpened?.Invoke(); })
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        public void Close(QuestPanelSettings settings, Action onClosed)
        {
            Kill();
            IsBusy = true;

            _sequence = DOTween.Sequence()
                .Append(DOVirtual.Float(_shown, 0f, settings.LiftDuration, Apply).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    _layer.style.display = DisplayStyle.None;
                    IsOpen = false;
                    IsBusy = false;
                    onClosed?.Invoke();
                })
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        // NOTE: The dim fades with the lift; the note grows from LiftStartScale (OutBack gives a small overshoot).
        private void Apply(float shown)
        {
            _shown = shown;
            var startScale = GameConfig.HasInstance ? GameConfig.Instance.QuestPanel.LiftStartScale : 0.7f;
            var scale = Mathf.LerpUnclamped(startScale, 1f, shown);

            _layer.style.opacity = Mathf.Clamp01(shown);
            _note.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        // NOTE: Member lines are pooled — created on first need, reused and hidden afterward.
        private void ShowMembers(IReadOnlyList<Adventurer> members)
        {
            var lines = members.Count == 0 ? 1 : members.Count;
            while (_memberLines.Count < lines)
            {
                OutlinedText line = new();
                line.AddToClassList("note-ink");
                line.AddToClassList("note-member");
                _members.Add(line);
                _memberLines.Add(line);
            }

            for (var i = 0; i < _memberLines.Count; i++)
            {
                var visible = i < lines;
                _memberLines[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible)
                    continue;

                _memberLines[i].Text = members.Count == 0
                    ? NoMembersText
                    : $"{members[i].Name} · {members[i].Class.DisplayName} · Lv {members[i].Level}";
            }
        }

        private void Kill()
        {
            if (_sequence.IsActive())
                _sequence.Kill();
        }
    }
}