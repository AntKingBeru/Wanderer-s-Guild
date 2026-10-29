// Quest board panel: a cork board of 10 slots (3 + guild rank open); click a note to lift it; Cancel asks first (−1 rep).
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class QuestBoardPanel : MenuPanel
    {
        private const int Columns = 5;

        private readonly List<QuestNoteView> _notes = new();
        private readonly List<Button> _noteButtons = new();
        private readonly List<Button> _buttons = new();
        private readonly List<VisualElement> _rowOrder = new();
        private readonly List<VisualElement> _columnOrder = new();
        private QuestBoardLayout _layout;
        private QuestNoteDetailView _detail;
        private OutlinedText _title;
        private Button _close;
        private InlineConfirmation _confirm;
        private int _liftedQuestId = -1;
        private int _pendingCancelId = -1;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override IReadOnlyList<Button> ListButtons => _noteButtons;
        protected override Focusable DefaultFocus => FirstPinnedNote() ?? _close;

        private bool IsBoardActive => IsInteractable && !_detail.IsOpen && !_confirm.IsOpen;
        private bool IsDetailActive => IsInteractable && _detail.IsOpen && !_detail.IsBusy && !_confirm.IsOpen;
        private bool IsConfirmActive => IsInteractable && _confirm.IsOpen;

        private static long Now => RequestManager.NowMinutes;
        private static QuestPanelSettings PanelSettings => GameConfig.Instance.QuestPanel;
        private static QuestSettings Settings => GameConfig.Instance.Quests;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>("qboard-title");
            var grid = documentRoot.Q<VisualElement>("qboard-grid");
            _close = documentRoot.Q<Button>("qboard-close");
            _detail = QuestNoteDetailView.Bind(documentRoot, gameObject);
            _confirm = InlineConfirmation.Bind(documentRoot);

            if (_title == null || grid == null || _close == null || _detail == null || _confirm == null)
                return false;

            var slots = GameConfig.HasInstance ? Settings.BoardSlots : 10;
            _layout = new QuestBoardLayout(slots);

            for (var i = 0; i < slots; i++)
            {
                QuestNoteView note = new(Lift);
                grid.Add(note.Button);
                _notes.Add(note);
                _noteButtons.Add(note.Button);
            }

            _buttons.Clear();
            _buttons.Add(_close);
            _buttons.Add(_detail.Back);
            _buttons.Add(_detail.Cancel);
            _buttons.AddRange(_confirm.Buttons);

            BuildNavigationOrders(slots);
            return true;
        }

        protected override void OnInitialize()
        {
            _close.clicked += Close;
            _detail.Back.clicked += PutBack;
            _detail.Cancel.clicked += AskCancel;
            _confirm.Initialize();

            // NOTE: Left/Right walks the rows, Up/Down the columns; each passes the other axis through, so every arrow
            // has exactly one owner. Locked and empty slots are disabled and skipped.
            AddPanelManipulator(new MenuNavigation(_rowOrder, () => IsBoardActive, null, NavigationAxis.Horizontal, passThroughCrossAxis: true));
            AddPanelManipulator(new MenuNavigation(_columnOrder, () => IsBoardActive, null, passThroughCrossAxis: true));
            AddPanelManipulator(new MenuNavigation(new VisualElement[] { _detail.Back, _detail.Cancel }, () => IsDetailActive, null,
                                                   NavigationAxis.Horizontal));
            AddPanelManipulator(new MenuNavigation(_confirm.Buttons, () => IsConfirmActive, null, NavigationAxis.Horizontal));

            GameEventsRelay.OnQuestPosted.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnAdventurerLeveledUp.AddListener(OnAdventurerLeveledUp);
            GameEventsRelay.OnGuildRankUp.AddListener(OnGuildRankUp);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnQuestPosted.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnAdventurerLeveledUp.RemoveListener(OnAdventurerLeveledUp);
            GameEventsRelay.OnGuildRankUp.RemoveListener(OnGuildRankUp);
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            _confirm.HideImmediate();
            _detail.HideImmediate();
            _liftedQuestId = -1;
            _pendingCancelId = -1;
            Refresh();
        }

        // NOTE: Esc order: the cancel prompt (No), then the lifted note (Back), then the board itself.
        protected override void OnCancel()
        {
            if (_confirm.IsOpen)
                _confirm.Cancel();
            else if (_detail.IsOpen)
                PutBack();
            else
                Close();
        }

        private void Close()
        {
            if (IsBoardActive)
                Host.Close(Id);
        }

        // ---------- Lifted note ----------

        private void Lift(QuestNoteView note)
        {
            if (!IsBoardActive || note.Quest == null)
                return;

            _liftedQuestId = note.Quest.Id;
            ShowDetail(note.Quest);
            _detail.Open(PanelSettings, () => _detail.Back.Focus());
        }

        private void PutBack()
        {
            if (!_detail.IsOpen || _detail.IsBusy)
                return;

            var questId = _liftedQuestId;
            _detail.Close(PanelSettings, () =>
            {
                _liftedQuestId = -1;
                FocusNoteOr(questId);
            });
        }

        private void ShowDetail(Quest quest)
        {
            if (!QuestManager.HasInstance)
                return;

            var members = QuestManager.Instance.MembersOf(quest);
            var chance = QuestManager.Instance.SuccessChance(quest);
            _detail.Show(quest, members, chance, Now, GameConfig.Instance.GuildRanks.ColorOf(quest.Request.RecommendedRank));
        }

        private void AskCancel()
        {
            if (!IsDetailActive || _liftedQuestId < 0 || !QuestManager.HasInstance)
                return;

            var questId = _liftedQuestId;
            var reputation = GameConfig.Instance.Requests.DismissReputation;
            var confirmed = false;
            _pendingCancelId = questId;

            _confirm.Show($"Cancel this quest?\n({reputation:+#;-#;0} reputation)",
                          onConfirm: () => confirmed = true,
                          onClosed: () =>
                          {
                              _pendingCancelId = -1;
                              if (confirmed && QuestManager.HasInstance)
                                  QuestManager.Instance.Cancel(questId); // Refused if it departed or ended meanwhile.

                              if (_detail.IsOpen) _detail.Cancel.Focus();
                              else FocusNoteOr(-1);
                          });
        }

        // ---------- Board ----------

        // NOTE: Slots ≥ the open count are locked (plank with the unlocking rank). A lifted note whose quest has gone
        // is hidden at once; otherwise it's refreshed (READY when the party fills, TAKEN once it leaves).
        private void Refresh()
        {
            var quests = QuestManager.HasInstance ? QuestManager.Instance.Quests : System.Array.Empty<Quest>();
            var open = QuestManager.HasInstance ? QuestManager.Instance.OpenSlots : 0;
            var ranks = GameConfig.Instance.GuildRanks;

            _layout.Update(quests, open);
            _title.Text = $"Quest Board {quests.Count}/{open}";

            for (var i = 0; i < _notes.Count; i++)
            {
                var quest = _layout.Slots[i];
                if (quest != null)
                    _notes[i].ShowQuest(quest, Now, ranks.ColorOf(quest.Request.RecommendedRank));
                else if (i >= open)
                    _notes[i].ShowLocked(Settings.TryGetUnlockRank(i, out GuildRank rank) ? $"Rank {rank}" : "Locked");
                else
                    _notes[i].ShowEmpty();
            }

            if (!_detail.IsOpen)
                return;

            var lifted = QuestManager.HasInstance ? QuestManager.Instance.Find(_liftedQuestId) : null;
            if (lifted != null)
            {
                ShowDetail(lifted);
                return;
            }

            if (_confirm.IsOpen) _confirm.Cancel();
            _detail.HideImmediate();
            _liftedQuestId = -1;
            FocusNoteOr(-1);
        }

        private void BuildNavigationOrders(int slots)
        {
            _rowOrder.Clear();
            _columnOrder.Clear();

            foreach (var note in _notes)
                _rowOrder.Add(note.Button);

            var rows = Mathf.CeilToInt(slots / (float)Columns);
            for (var column = 0; column < Columns; column++)
            {
                for (var row = 0; row < rows; row++)
                {
                    var index = row * Columns + column;
                    if (index < slots)
                        _columnOrder.Add(_notes[index].Button);
                }
            }

            _rowOrder.Add(_close);
            _columnOrder.Add(_close);
        }

        private Button FirstPinnedNote()
        {
            return (from note in _notes where note.Quest != null select note.Button).FirstOrDefault();
        }

        private void FocusNoteOr(int questId)
        {
            var slot = questId >= 0 ? _layout.SlotOf(questId) : -1;
            var target = slot >= 0 && _notes[slot].Quest != null ? _notes[slot].Button : DefaultFocus;
            target?.Focus();
        }

        // NOTE: Becoming Ready doesn't close the cancel prompt (a ready quest can still be canceled) — only departing does.
        private void OnQuestChanged(Quest quest)
        {
            if (quest.Id == _pendingCancelId && !quest.IsBeforeDeparture && _confirm.IsOpen)
                _confirm.Cancel();

            if (IsOpen)
                Refresh();
        }

        private void OnQuestEnded(Quest quest, QuestEndReason _)
        {
            if (quest.Id == _pendingCancelId && _confirm.IsOpen)
                _confirm.Cancel();

            if (IsOpen)
                Refresh();
        }

        private void OnHourStarted(GameDateTime _)
        {
            if (IsOpen)
                Refresh();
        }

        private void OnAdventurerLeveledUp(Adventurer _)
        {
            if (IsOpen)
                Refresh();
        }

        private void OnGuildRankUp(GuildRank _)
        {
            if (IsOpen)
                Refresh();
        }
    }
}