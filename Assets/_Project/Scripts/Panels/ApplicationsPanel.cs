// Applications screen: quests with applicants (ready first) + details; decline one or all, choose a stocked potion of the
// quest's category, approve a full party.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class ApplicationsPanel : MenuPanel
    {
        private const string EmptyMessage = "No applications right now.";

        private readonly List<Button> _buttons = new();
        private readonly List<VisualElement> _navigationOrder = new();
        private readonly List<Quest> _sorted = new();
        private readonly List<PotionKey> _stocked = new();
        private OutlinedText _title;
        private ApplicationListView _list;
        private ApplicationDetailsView _details;
        private Button _close;
        private int _selectedId = -1;
        private int _selectedIndex;
        private PotionKey _potion;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override IReadOnlyList<Button> ListButtons => _list.Buttons;
        protected override Focusable DefaultFocus => (Focusable)_list.ButtonFor(_selectedId) ?? _close;

        private static long Now => RequestManager.NowMinutes;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>("app-panel-title");
            var scroll = documentRoot.Q<ScrollView>("app-list");
            var listEmpty = documentRoot.Q<OutlinedText>("app-list-empty");
            _close = documentRoot.Q<Button>("app-close");

            var settings = GameConfig.HasInstance ? GameConfig.Instance.Quests : null;
            var rows = settings?.BoardSlots ?? 10;
            var maxApplicants = settings != null
                ? Mathf.Max(settings.PartySizeLimits(GuildRank.F).y, settings.PartySizeLimits(GuildRank.National).y)
                : 7;

            _details = ApplicationDetailsView.Bind(documentRoot, maxApplicants, Decline);

            if (_title == null || scroll == null || listEmpty == null || _close == null || _details == null)
                return false;

            _list = new ApplicationListView(scroll, listEmpty, rows, Select);

            _buttons.Clear();
            _buttons.AddRange(_details.DeclineButtons);
            _buttons.Add(_details.Potion);
            _buttons.Add(_details.DeclineAll);
            _buttons.Add(_details.Approve);
            _buttons.Add(_close);

            // NOTE: Up/Down walks quests → applicants' Decline → Potions → Decline all → Approve → Close; hidden/disabled
            // ones are skipped.
            _navigationOrder.Clear();
            _navigationOrder.AddRange(_list.Buttons);
            _navigationOrder.AddRange(_details.DeclineButtons);
            _navigationOrder.Add(_details.Potion);
            _navigationOrder.Add(_details.DeclineAll);
            _navigationOrder.Add(_details.Approve);
            _navigationOrder.Add(_close);
            return true;
        }

        protected override void OnInitialize()
        {
            _details.DeclineAll.clicked += DeclineAll;
            _details.Approve.clicked += Approve;
            _details.Potion.clicked += CyclePotion;
            _close.clicked += Close;

            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsInteractable, null));

            GameEventsRelay.OnQuestPosted.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestPartyReady.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnAdventurerLeveledUp.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerChanged.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnPotionStockChanged.AddListener(RefreshIfOpen);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnQuestPosted.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestPartyReady.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnAdventurerLeveledUp.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerChanged.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnPotionStockChanged.RemoveListener(RefreshIfOpen);
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            _potion = PotionKey.None;
            Refresh();
        }

        // NOTE: Esc/Close closes this screen — when opened from the reception desk, the host returns there.
        protected override void OnCancel() => Close();

        private void Close()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        // NOTE: Selection only changes on click/Submit (not on hover-focus). A new quest starts with no potion.
        private void Select(Quest quest)
        {
            if (!IsInteractable || quest == null)
                return;

            if (quest.Id != _selectedId)
                _potion = PotionKey.None;
            _selectedId = quest.Id;
            Refresh();
        }

        // NOTE: None → each stocked potion of this quest's category (lowest tier first) → None.
        private void CyclePotion()
        {
            if (!IsInteractable || !QuestManager.HasInstance || !AlchemyManager.HasInstance)
                return;

            var quest = QuestManager.Instance.Find(_selectedId);
            if (quest == null)
                return;

            Alchemy.StockedFor(quest.Request.Category, AlchemyManager.Instance.Stock, _stocked);
            var index = _potion.IsNone ? -1 : _stocked.IndexOf(_potion);
            _potion = index + 1 < _stocked.Count ? _stocked[index + 1] : PotionKey.None;
            Refresh();
        }

        // NOTE: Each action goes through QuestManager, which refuses anything no longer valid (expired, departed); the
        // quest events then refresh this screen.
        private void Decline(Adventurer adventurer)
        {
            if (!IsInteractable || adventurer == null || !QuestManager.HasInstance)
                return;
            QuestManager.Instance.Decline(_selectedId, adventurer.Id);
            RefreshAndKeepFocus();
        }

        private void DeclineAll()
        {
            if (!IsInteractable || !QuestManager.HasInstance)
                return;
            QuestManager.Instance.DeclineAll(_selectedId);
            RefreshAndKeepFocus();
        }

        // NOTE: Approve re-checks the potion (category, stock); after it the choice goes back to None.
        private void Approve()
        {
            if (!IsInteractable || !QuestManager.HasInstance)
                return;
            if (QuestManager.Instance.Approve(_selectedId, _potion))
                _potion = PotionKey.None;
            RefreshAndKeepFocus();
        }

        // NOTE: Only quests before departure with at least one applicant; Ready first, then soonest expiry, then id.
        private void Refresh()
        {
            _sorted.Clear();
            if (QuestManager.HasInstance)
            {
                foreach (var quest in QuestManager.Instance.Quests)
                {
                    if (quest.IsBeforeDeparture && quest.MemberIds.Count > 0)
                        _sorted.Add(quest);
                }
            }
            _sorted.Sort(CompareForList);

            var selected = ResolveSelection();
            _title.Text = $"Applications {_sorted.Count}";
            _list.Show(_sorted, _selectedId, GameConfig.Instance.GuildRanks);

            if (selected == null || !QuestManager.HasInstance)
            {
                _details.ShowEmpty(EmptyMessage);
                return;
            }

            ShowDetails(selected, QuestManager.Instance.MembersOf(selected));
        }

        // NOTE: The row shows when the alchemist exists or this category has potions in stock. A selected potion that's no
        // longer in stock falls back to None. A refusal shows the unchanged split/chance (they'd go without).
        private void ShowDetails(Quest quest, List<Adventurer> members)
        {
            var stock = AlchemyManager.HasInstance ? AlchemyManager.Instance.Stock : null;
            if (stock != null)
                Alchemy.StockedFor(quest.Request.Category, stock, _stocked);
            else
                _stocked.Clear();
            if (!_potion.IsNone && (_potion.Category != quest.Request.Category || !_stocked.Contains(_potion)))
                _potion = PotionKey.None;

            var split = QuestFormatter.GoldSplit(quest.Request, quest.Terms);
            var chance = QuestManager.Instance.SuccessChanceWith(quest, 0f);
            var preview = PotionPreview.Hidden;

            var showRow = Alchemy.Capacity > 0 || _stocked.Count > 0;
            if (showRow)
            {
                var label = PotionFormatter.Label(PotionKey.None, default, 0);
                var effect = _stocked.Count == 0 ? $"No {quest.Request.Category} potions in stock" : string.Empty;
                var warning = string.Empty;

                if (!_potion.IsNone && Alchemy.TryGetTier(_potion.Tier, out var tier))
                {
                    if (stock != null)
                        label = PotionFormatter.Label(_potion, tier, stock.Count(_potion));
                    effect = PotionFormatter.Effect(_potion, tier);

                    var refuser = Alchemy.FindRefuser(quest, members, tier, GameConfig.Instance.Quests);
                    if (refuser != null)
                    {
                        warning = ApplicationFormatter.PotionRefusal(refuser);
                    }
                    else
                    {
                        QuestTerms cut = new(Alchemy.ShareAfterCut(quest, tier.ShareCutPercent), quest.Terms.MinRank, quest.Terms.PartySize);
                        split = QuestFormatter.GoldSplit(quest.Request, cut);
                        chance = QuestManager.Instance.SuccessChanceWith(quest, tier.ChanceBonus);
                    }
                }

                preview = new PotionPreview(true, label, effect, warning, true);
            }

            _details.Show(quest, members, split, chance, Now, preview);
        }

        // NOTE: Declining or approving can hide the focused row/button; a disabled focused element would strand the
        // keyboard, so focus moves to the default target.
        private void RefreshAndKeepFocus()
        {
            Refresh();

            var focused = _close.focusController?.focusedElement;
            if (focused is VisualElement { enabledInHierarchy: false })
                DefaultFocus?.Focus();
        }

        private static int CompareForList(Quest a, Quest b)
        {
            bool aReady = a.State == QuestState.Ready, bReady = b.State == QuestState.Ready;
            if (aReady != bReady)
                return aReady ? -1 : 1;
            return a.ExpiresAtMinutes != b.ExpiresAtMinutes ? a.ExpiresAtMinutes.CompareTo(b.ExpiresAtMinutes) : a.Id.CompareTo(b.Id);
        }

        private Quest ResolveSelection()
        {
            for (var i = 0; i < _sorted.Count; i++)
            {
                if (_sorted[i].Id != _selectedId)
                    continue;

                _selectedIndex = i;
                return _sorted[i];
            }

            _potion = PotionKey.None;
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

        private void OnQuestChanged(Quest _) => RefreshIfOpen();
        private void OnQuestEnded(Quest _, QuestEndReason __) => RefreshIfOpen();
        private void OnHourStarted(GameDateTime _) => RefreshIfOpen();
        private void OnAdventurerEvent(Adventurer _) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen)
                RefreshAndKeepFocus();
        }
    }
}