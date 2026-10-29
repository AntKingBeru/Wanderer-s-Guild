// One board slot: a pinned note (badge, category icon, reward, time, READY/TAKEN stamp), an empty pin mark, or a locked plank.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class QuestNoteView
    {
        private const string PaperClass = "quest-note--paper";
        private const string EmptyClass = "quest-note--empty";
        private const string LockedClass = "quest-note--locked";
        private const string TakenClass = "quest-note--taken";
        private const string ReadyClass = "quest-note--ready";
        private const string UrgentClass = "quest-note--urgent";
        private const string PlaceholderClass = "quest-note__icon--placeholder";

        private readonly VisualElement _badge;
        private readonly OutlinedText _badgeLabel;
        private readonly VisualElement _icon;
        private readonly OutlinedText _iconFallback;
        private readonly OutlinedText _reward;
        private readonly OutlinedText _time;
        private readonly OutlinedText _stamp;
        private readonly OutlinedText _plankLabel;

        public Button Button { get; }
        public Quest Quest { get; private set; }

        public QuestNoteView(Action<QuestNoteView> onClicked)
        {
            // NOTE: Fully qualified — this class's own "Button" property would otherwise hide the type's static member.
            Button = new Button(() => onClicked(this));
            Button.RemoveFromClassList(Button.ussClassName);
            Button.AddToClassList("quest-note");

            var paper = Element("quest-note__paper");
            _icon = Element("quest-note__icon");
            _iconFallback = Text("quest-note__icon-letters");
            _icon.Add(_iconFallback);
            _reward = Text("quest-note__reward");
            _time = Text("quest-note__time");
            _stamp = Text("quest-note__stamp");

            paper.Add(_icon);
            paper.Add(_reward);
            paper.Add(_time);
            paper.Add(_stamp);

            _badge = Element("quest-note__badge");
            _badgeLabel = new OutlinedText();
            _badge.Add(_badgeLabel);

            var plank = Element("quest-note__plank");
            _plankLabel = Text("quest-note__plank-label");
            plank.Add(_plankLabel);

            Button.Add(paper);
            Button.Add(_badge);
            Button.Add(Element("quest-note__pin"));
            Button.Add(plank);

            ShowEmpty();
        }

        public void ShowQuest(Quest quest, long nowMinutes, Color rankColor)
        {
            Quest = quest;

            _badge.style.backgroundColor = rankColor;
            _badgeLabel.Text = RankFormatter.BadgeLabel(quest.Request.RecommendedRank);
            ShowCategory(quest.Request.Category);
            _reward.Text = QuestFormatter.NoteReward(quest);
            _time.Text = QuestFormatter.NoteTime(quest, nowMinutes);
            _stamp.Text = QuestFormatter.Stamp(quest);

            SetState(PaperClass, enabled: true);
            Button.EnableInClassList(ReadyClass, quest.State == QuestState.Ready);
            Button.EnableInClassList(TakenClass, quest.State == QuestState.InProgress);
            Button.EnableInClassList(UrgentClass, QuestFormatter.IsUrgent(quest, nowMinutes));
        }

        public void ShowEmpty() => SetState(EmptyClass, enabled: false);

        public void ShowLocked(string unlockText)
        {
            _plankLabel.Text = unlockText;
            SetState(LockedClass, enabled: false);
        }

        // NOTE: Set on every show — pooled notes are reused for other categories. No sprite (or no icon asset) shows
        // the category's first two letters instead.
        private void ShowCategory(QuestCategory category)
        {
            var icons = GameConfig.HasInstance ? GameConfig.Instance.QuestPanel.CategoryIcons : null;
            var sprite = icons ? icons.IconFor(category) : null;

            _icon.style.backgroundImage = sprite ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
            _icon.EnableInClassList(PlaceholderClass, !sprite);
            _iconFallback.Text = QuestFormatter.CategoryShort(category);
            _iconFallback.style.display = !sprite ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // NOTE: Empty and locked slots are disabled, so keyboard navigation skips them and clicks do nothing.
        private void SetState(string stateClass, bool enabled)
        {
            if (!enabled)
                Quest = null;

            Button.EnableInClassList(PaperClass, stateClass == PaperClass);
            Button.EnableInClassList(EmptyClass, stateClass == EmptyClass);
            Button.EnableInClassList(LockedClass, stateClass == LockedClass);
            if (!enabled)
            {
                Button.RemoveFromClassList(TakenClass);
                Button.RemoveFromClassList(ReadyClass);
                Button.RemoveFromClassList(UrgentClass);
            }
            Button.SetEnabled(enabled);
        }

        private static VisualElement Element(string className)
        {
            VisualElement element = new() { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            return element;
        }

        private static OutlinedText Text(string className)
        {
            OutlinedText text = new();
            text.AddToClassList(className);
            return text;
        }
    }
}