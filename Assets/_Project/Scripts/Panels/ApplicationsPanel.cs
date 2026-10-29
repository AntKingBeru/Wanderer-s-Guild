// Applications screen: quests with applicants (ready first) + details; decline one or all applicants, approve a full party.
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
        private OutlinedText _title;
        private ApplicationListView _list;
        private ApplicationDetailsView _details;
        private Button _close;
        private int _selectedId = -1;
        private int _selectedIndex;

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

            QuestSettings settings = GameConfig.HasInstance ? GameConfig.Instance.Quests : null;
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
            _buttons.Add(_details.DeclineAll);
            _buttons.Add(_details.Approve);
            _buttons.Add(_close);

            // NOTE: Up/Down walks quests → applicants' Decline → Decline all → Approve → Close; hidden/disabled ones are skipped.
            _navigationOrder.Clear();
            _navigationOrder.AddRange(_list.Buttons);
            _navigationOrder.AddRange(_details.DeclineButtons);
            _navigationOrder.Add(_details.DeclineAll);
            _navigationOrder.Add(_details.Approve);
            _navigationOrder.Add(_close);
            return true;
        }

        protected override void OnInitialize()
        {
            _details.DeclineAll.clicked += DeclineAll;
            _details.Approve.clicked += Approve;
            _close.clicked += Close;

            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsInteractable, null));

            GameEventsRelay.OnQuestPosted.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestPartyReady.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnAdventurerLeveledUp.AddListener(OnAdventurerEvent);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnQuestPosted.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestPartyReady.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnAdventurerLeveledUp.RemoveListener(OnAdventurerEvent);
            base.OnDestroy();
        }

        protected override void OnOpening() => Refresh();

        // NOTE: Esc/Close closes this screen — when opened from the reception desk, the host returns there.
        protected override void OnCancel() => Close();

        private void Close()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        // NOTE: Selection only changes on click/Submit (not on hover-focus).
        private void Select(Quest quest)
        {
            if (!IsInteractable || quest == null)
                return;

            _selectedId = quest.Id;
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

        private void Approve()
        {
            if (!IsInteractable || !QuestManager.HasInstance)
                return;
            QuestManager.Instance.Approve(_selectedId);
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

            if (selected != null && QuestManager.HasInstance)
            {
                var applicants = QuestManager.Instance.MembersOf(selected);
                var chance = QuestManager.Instance.SuccessChance(selected);
                _details.Show(selected, applicants, chance, Now);
            }
            else
            {
                _details.ShowEmpty(EmptyMessage);
            }
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