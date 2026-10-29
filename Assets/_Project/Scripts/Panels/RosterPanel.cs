// Adventurer roster panel: sorted list (rank, then level) + details; Promote asks first, then starts the rank exam.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class RosterPanel : MenuPanel
    {
        private const string EmptyMessage = "No adventurers yet.";

        private readonly List<Button> _buttons = new();
        private readonly List<VisualElement> _navigationOrder = new();
        private readonly List<Adventurer> _sorted = new();
        private OutlinedText _title;
        private RosterListView _list;
        private AdventurerDetailsView _details;
        private InlineConfirmation _confirm;
        private Button _close;
        private int _selectedId = -1;
        private int _selectedIndex;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override IReadOnlyList<Button> ListButtons => _list.Buttons;
        protected override Focusable DefaultFocus => (Focusable)_list.ButtonFor(_selectedId) ?? _close;

        private bool IsPanelActive => IsInteractable && !_confirm.IsOpen;
        private bool IsConfirmActive => IsInteractable && _confirm.IsOpen;

        private static AdventurerSettings Settings => GameConfig.Instance.Adventurers;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>("roster-title");
            var scroll = documentRoot.Q<ScrollView>("roster-list");
            var listEmpty = documentRoot.Q<OutlinedText>("roster-list-empty");
            _close = documentRoot.Q<Button>("roster-close");
            _details = AdventurerDetailsView.Bind(documentRoot);
            _confirm = InlineConfirmation.Bind(documentRoot);

            if (_title == null || scroll == null || listEmpty == null || _close == null || _details == null || _confirm == null)
                return false;

            // NOTE: One pooled row per roster slot — the roster can never hold more.
            var capacity = GameConfig.HasInstance ? Settings.Capacity : 6;
            _list = new RosterListView(scroll, listEmpty, capacity, Select);

            _buttons.Clear();
            _buttons.Add(_details.Promote);
            _buttons.Add(_close);
            _buttons.AddRange(_confirm.Buttons);

            _navigationOrder.Clear();
            _navigationOrder.AddRange(_list.Buttons);
            _navigationOrder.Add(_details.Promote);
            _navigationOrder.Add(_close);
            return true;
        }

        // NOTE: Promotion eligibility depends on gold, guild rank, level and status — all of these refresh the panel.
        protected override void OnInitialize()
        {
            _details.Promote.clicked += AskPromote;
            _close.clicked += Close;
            _confirm.Initialize();

            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsPanelActive, null));
            AddPanelManipulator(new MenuNavigation(_confirm.Buttons, () => IsConfirmActive, null, NavigationAxis.Horizontal));

            GameEventsRelay.OnAdventurerJoined.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerLeveledUp.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerChanged.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnPromotionExamEnded.AddListener(OnExamEnded);
            GameEventsRelay.OnGoldChanged.AddListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.AddListener(OnGuildRankUp);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnAdventurerJoined.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerLeveledUp.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerChanged.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnPromotionExamEnded.RemoveListener(OnExamEnded);
            GameEventsRelay.OnGoldChanged.RemoveListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.RemoveListener(OnGuildRankUp);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            _confirm.HideImmediate();
            Refresh();
        }

        // NOTE: Esc answers the promote prompt (No) first; otherwise it closes the roster.
        protected override void OnCancel()
        {
            if (_confirm.IsOpen)
                _confirm.Cancel();
            else
                Close();
        }

        private void Close()
        {
            if (IsPanelActive)
                Host.Close(Id);
        }

        // NOTE: Selection only changes on click/Submit (not on hover-focus), so the details never jump under the mouse.
        private void Select(Adventurer adventurer)
        {
            if (!IsPanelActive || adventurer == null)
                return;

            _selectedId = adventurer.Id;
            Refresh();
        }

        // NOTE: StartExam re-checks eligibility and spends with TrySpend — if anything changed meanwhile, nothing happens
        // and the hint shows why.
        private void AskPromote()
        {
            if (!IsPanelActive || !PromotionManager.HasInstance || !AdventurerManager.HasInstance)
                return;

            var adventurer = AdventurerManager.Instance.Find(_selectedId);
            if (adventurer == null)
                return;

            var check = PromotionManager.Instance.Check(adventurer);
            if (!check.CanPromote)
                return;

            var adventurerId = adventurer.Id;
            var confirmed = false;
            _confirm.Show($"Take the Rank {check.Target} exam?\n(-{check.Fee:N0}g)",
                          onConfirm: () => confirmed = true,
                          onClosed: () =>
                          {
                              if (confirmed && PromotionManager.HasInstance)
                                  PromotionManager.Instance.StartExam(adventurerId);

                              Refresh();
                              Focus((Focusable)_list.ButtonFor(_selectedId) ?? _close);
                          });
        }

        // NOTE: Sorted by rank, then level (highest first), then name. The selection is kept by adventurer id.
        private void Refresh()
        {
            _sorted.Clear();
            if (AdventurerManager.HasInstance)
                _sorted.AddRange(AdventurerManager.Instance.Adventurers);
            _sorted.Sort(CompareForRoster);

            var selected = ResolveSelection();
            var capacity = AdventurerManager.HasInstance ? AdventurerManager.Instance.Capacity : Settings.Capacity;

            _title.Text = $"Adventurers {_sorted.Count}/{capacity}";
            _list.Show(_sorted, _selectedId, GameConfig.Instance.GuildRanks);

            if (selected == null)
            {
                _details.ShowEmpty(EmptyMessage);
                return;
            }

            var check = PromotionManager.HasInstance ? PromotionManager.Instance.Check(selected) : PromotionCheck.HighestRank();
            _details.Show(selected, Settings.Experience, check, GameConfig.Instance.Promotion, RequestManager.NowMinutes);
        }

        private static int CompareForRoster(Adventurer a, Adventurer b)
        {
            if (a.Rank != b.Rank) return b.Rank.CompareTo(a.Rank);
            return a.Level != b.Level ? b.Level.CompareTo(a.Level) : string.CompareOrdinal(a.Name, b.Name);
        }

        private Adventurer ResolveSelection()
        {
            for (var i = 0; i < _sorted.Count; i++)
            {
                if (_sorted[i].Id != _selectedId)
                    continue;

                _selectedIndex = i;
                return _sorted[i];
            }

            if (_sorted.Count == 0)
            {
                _selectedId = -1;
                _selectedIndex = 0;
                return null;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _sorted.Count - 1);
            _selectedId = _sorted[_selectedIndex].Id;
            return _sorted[_selectedIndex];
        }

        private void OnAdventurerEvent(Adventurer _) => RefreshIfOpen();
        private void OnExamEnded(Adventurer _, GuildRank __, bool ___) => RefreshIfOpen();
        private void OnGoldChanged(long _, long __) => RefreshIfOpen();
        private void OnGuildRankUp(GuildRank _) => RefreshIfOpen();
        private void OnHourStarted(GameDateTime _) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen)
                Refresh();
        }

        private static void Focus(Focusable target) => target?.Focus();
    }
}