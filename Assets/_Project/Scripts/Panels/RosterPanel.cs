// Adventurer roster panel: sorted list (rank, then level) + details; Promote asks first, then starts the rank exam.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class RosterPanel : MenuPanel
    {
        private const string EmptyMessage = "No adventurers yet.";

        // NOTE: Rows are created once and can't grow afterward (MenuPanel sets them up in Initialize) — far more than any
        // realistic guild; past this, the extra adventurers aren't listed and a warning says to raise it.
        private const int RowPool = 48;
        
        private static int _selectOnNextOpen = -1;
        
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
        private bool _warnedPool;

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

            _list = new RosterListView(scroll, listEmpty, RowPool, Select);

            _buttons.Clear();
            _buttons.Add(_details.Promote);
            _buttons.Add(_close);
            _buttons.AddRange(_confirm.Buttons);
            _buttons.Add(_details.Disband);

            _navigationOrder.Clear();
            _navigationOrder.AddRange(_list.Buttons);
            _navigationOrder.Add(_details.Promote);
            _navigationOrder.Add(_details.Disband);
            _navigationOrder.Add(_close);
            return true;
        }

        // NOTE: Promotion eligibility depends on gold, guild rank, level and status — all of these refresh the panel.
        protected override void OnInitialize()
        {
            _details.Promote.clicked += AskPromote;
            _close.clicked += Close;
            _confirm.Initialize();
            _details.Disband.clicked += AskDisband;

            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsPanelActive, null));
            AddPanelManipulator(new MenuNavigation(_confirm.Buttons, () => IsConfirmActive, null, NavigationAxis.Horizontal));

            GameEventsRelay.OnAdventurerJoined.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerLeveledUp.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerChanged.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerCapacityChanged.AddListener(OnCapacityChanged);
            GameEventsRelay.OnPromotionExamEnded.AddListener(OnExamEnded);
            GameEventsRelay.OnGoldChanged.AddListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.AddListener(OnGuildRankUp);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnPartyFormed.AddListener(OnPartyEvent);
            GameEventsRelay.OnPartyChanged.AddListener(OnPartyEvent);
            GameEventsRelay.OnPartyDisbanded.AddListener(OnPartyDisbanded);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnAdventurerJoined.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerLeveledUp.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerChanged.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerCapacityChanged.RemoveListener(OnCapacityChanged);
            GameEventsRelay.OnPromotionExamEnded.RemoveListener(OnExamEnded);
            GameEventsRelay.OnGoldChanged.RemoveListener(OnGoldChanged);
            GameEventsRelay.OnGuildRankUp.RemoveListener(OnGuildRankUp);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnPartyFormed.RemoveListener(OnPartyEvent);
            GameEventsRelay.OnPartyChanged.RemoveListener(OnPartyEvent);
            GameEventsRelay.OnPartyDisbanded.RemoveListener(OnPartyDisbanded);
            base.OnDestroy();
        }
        
        // The next time the roster opens, select this adventurer (one-shot — e.g. from the world card).
        public static void SelectOnNextOpen(int adventurerId) => _selectOnNextOpen = adventurerId;

        protected override void OnOpening()
        {
            _confirm.HideImmediate();
            if (_selectOnNextOpen >= 0)
            {
                _selectedId = _selectOnNextOpen;
                _selectOnNextOpen = -1;
            }
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

        // NOTE: Sorted by rank, then level (highest first), then name. The selection is kept by adventurer id, among the
        // listed rows only.
        private void Refresh()
        {
            _sorted.Clear();
            if (AdventurerManager.HasInstance)
                _sorted.AddRange(AdventurerManager.Instance.Adventurers);
            _sorted.Sort(CompareForRoster);

            if (_sorted.Count > _list.RowCount && !_warnedPool)
            {
                _warnedPool = true;
                Debug.LogWarning($"[RosterPanel] {_sorted.Count} adventurers but only {_list.RowCount} rows; raise RowPool.", this);
            }

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
            var partyLine = PartyManager.HasInstance ? PartyManager.Instance.LineFor(selected) : string.Empty;
            _details.Show(selected, Settings.Experience, check, GameConfig.Instance.Promotion, RequestManager.NowMinutes, partyLine);
        }

        private static int CompareForRoster(Adventurer a, Adventurer b)
        {
            if (a.Rank != b.Rank)
                return b.Rank.CompareTo(a.Rank);
            return a.Level != b.Level ? b.Level.CompareTo(a.Level) : string.CompareOrdinal(a.Name, b.Name);
        }

        private Adventurer ResolveSelection()
        {
            var visible = Mathf.Min(_sorted.Count, _list.RowCount);
            for (var i = 0; i < visible; i++)
            {
                if (_sorted[i].Id != _selectedId)
                    continue;

                _selectedIndex = i;
                return _sorted[i];
            }

            if (visible == 0)
            {
                _selectedId = -1;
                _selectedIndex = 0;
                return null;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, visible - 1);
            _selectedId = _sorted[_selectedIndex].Id;
            return _sorted[_selectedIndex];
        }

        private void OnAdventurerEvent(Adventurer _) => RefreshIfOpen();
        private void OnCapacityChanged(int _) => RefreshIfOpen();
        private void OnExamEnded(Adventurer _, GuildRank __, bool ___) => RefreshIfOpen();
        private void OnGoldChanged(long _, long __) => RefreshIfOpen();
        private void OnGuildRankUp(GuildRank _) => RefreshIfOpen();
        private void OnHourStarted(GameDateTime _) => RefreshIfOpen();
        
        // NOTE: Asks first; the party may have broken up meanwhile — Disband is a no-op then.
        private void AskDisband()
        {
            if (!IsPanelActive || !PartyManager.HasInstance)
                return;

            var party = PartyManager.Instance.PartyOf(_selectedId);
            if (party == null)
                return;

            var partyId = party.Id;
            var confirmed = false;
            _confirm.Show($"Disband {party.Name}?",
                onConfirm: () => confirmed = true,
                onClosed: () =>
                {
                    if (confirmed && PartyManager.HasInstance)
                        PartyManager.Instance.Disband(partyId);
                    Refresh();
                    Focus((Focusable)_list.ButtonFor(_selectedId) ?? _close);
                });
        }
        
        private void OnPartyEvent(GuildParty _) => RefreshIfOpen();
        private void OnPartyDisbanded(GuildParty _, PartyEndReason __) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen)
                Refresh();
        }

        private static void Focus(Focusable target) => target?.Focus();
    }
}