// Request board panel: sorted list + details; Accept opens the quest clipboard (needs a free quest-board slot); Dismiss asks first.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class RequestBoardPanel : MenuPanel
    {
        private const string TitleName = "board-title";
        private const string ListName = "board-list";
        private const string ListEmptyName = "board-list-empty";
        private const string CloseName = "board-close";

        private readonly List<Button> _buttons = new();
        private readonly List<VisualElement> _navigationOrder = new();
        private readonly List<ActiveRequest> _sorted = new();
        private OutlinedText _title;
        private RequestListView _list;
        private RequestDetailsView _details;
        private QuestClipboardView _clipboard;
        private QuestTermsOptions _termsOptions;
        private Button _close;
        private InlineConfirmation _confirm;
        private int _selectedId = -1;
        private int _selectedIndex;
        private int _pendingDismissId = -1;
        private int _clipboardRequestId = -1;
        private bool _isPosting;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override IReadOnlyList<Button> ListButtons => _list.Buttons;
        protected override Focusable DefaultFocus => (Focusable)_list.ButtonFor(_selectedId) ?? _close;

        private bool IsPanelActive => IsInteractable && !_confirm.IsOpen && !_clipboard.IsOpen;
        private bool IsConfirmActive => IsInteractable && _confirm.IsOpen;
        private bool IsClipboardActive => IsInteractable && _clipboard.IsOpen && !_clipboard.IsBusy && !_confirm.IsOpen;

        private static long Now => RequestManager.NowMinutes;
        private static GuildRankSettings Ranks => GameConfig.Instance.GuildRanks;
        private static QuestClipboardSettings ClipboardSettings => GameConfig.Instance.QuestClipboard;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>(TitleName);
            var scroll = documentRoot.Q<ScrollView>(ListName);
            var listEmpty = documentRoot.Q<OutlinedText>(ListEmptyName);
            _close = documentRoot.Q<Button>(CloseName);
            _details = RequestDetailsView.Bind(documentRoot);
            _clipboard = QuestClipboardView.Bind(documentRoot, gameObject);
            _confirm = InlineConfirmation.Bind(documentRoot);

            if (_title == null || scroll == null || listEmpty == null || _close == null ||
                _details == null || _clipboard == null || _confirm == null)
                return false;

            // NOTE: One pooled row per board slot — the board can never hold more.
            var capacity = GameConfig.HasInstance ? GameConfig.Instance.Requests.BoardCapacity : 10;
            _list = new RequestListView(scroll, listEmpty, capacity, Select);

            _buttons.Clear();
            _buttons.Add(_details.Accept);
            _buttons.Add(_details.Dismiss);
            _buttons.Add(_close);
            _buttons.Add(_clipboard.Back);
            _buttons.Add(_clipboard.Post);
            _buttons.AddRange(_confirm.Buttons);

            _navigationOrder.Clear();
            _navigationOrder.AddRange(_list.Buttons);
            _navigationOrder.Add(_details.Accept);
            _navigationOrder.Add(_details.Dismiss);
            _navigationOrder.Add(_close);
            return true;
        }

        protected override void OnInitialize()
        {
            _details.Accept.clicked += OpenClipboard;
            _details.Dismiss.clicked += AskDismiss;
            _close.clicked += Close;
            _clipboard.Back.clicked += CloseClipboard;
            _clipboard.Post.clicked += Post;
            _confirm.Initialize();

            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsPanelActive, null));
            AddPanelManipulator(new MenuNavigation(_confirm.Buttons, () => IsConfirmActive, null, NavigationAxis.Horizontal));

            // NOTE: Up/Down moves through the clipboard; Left/Right passes through to the cyclers (they change value),
            // except on Back/Post, where the second navigation swaps between the two buttons.
            AddPanelManipulator(new MenuNavigation(_clipboard.NavigationOrder, () => IsClipboardActive, null, passThroughCrossAxis: true));
            AddPanelManipulator(new MenuNavigation(new VisualElement[] { _clipboard.Back, _clipboard.Post },
                                                   () => IsClipboardActive && IsClipboardButtonFocused(), null,
                                                   NavigationAxis.Horizontal, passThroughCrossAxis: true));

            GameEventsRelay.OnRequestAdded.AddListener(OnRequestAdded);
            GameEventsRelay.OnRequestRemoved.AddListener(OnRequestRemoved);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnQuestPosted.AddListener(OnQuestBoardChanged);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnGuildRankUp.AddListener(OnGuildRankUp);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnRequestAdded.RemoveListener(OnRequestAdded);
            GameEventsRelay.OnRequestRemoved.RemoveListener(OnRequestRemoved);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnQuestPosted.RemoveListener(OnQuestBoardChanged);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnGuildRankUp.RemoveListener(OnGuildRankUp);
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            _confirm.HideImmediate();
            _clipboard.HideImmediate();
            _pendingDismissId = -1;
            _clipboardRequestId = -1;
            _isPosting = false;
            Refresh();
        }

        // NOTE: Esc order: the dismiss prompt (No), then the clipboard (Back), then the board itself.
        protected override void OnCancel()
        {
            if (_confirm.IsOpen)
                _confirm.Cancel();
            else if (_clipboard.IsOpen)
                CloseClipboard();
            else
                Close();
        }

        private void Close()
        {
            if (IsPanelActive)
                Host.Close(Id);
        }

        // NOTE: Selection only changes on click/Submit (not on hover-focus), so the details pane never jumps while the
        // mouse travels from the list to the buttons.
        private void Select(ActiveRequest request)
        {
            if (!IsPanelActive || request == null)
                return;

            _selectedId = request.InstanceId;
            Refresh();
        }

        // ---------- Clipboard ----------

        // NOTE: Never opens just to refuse — no free quest-board slot means no clipboard ("Accept" is disabled then too).
        private void OpenClipboard()
        {
            if (!IsPanelActive || !QuestManager.HasInstance || !QuestManager.Instance.HasFreeSlot)
                return;

            var request = FindSorted(_selectedId);
            if (request == null)
                return;

            var defaults = QuestManager.Instance.DefaultTerms(request.Data);
            _termsOptions = new QuestTermsOptions(request.Data, defaults, GameConfig.Instance.Quests);
            _clipboardRequestId = request.InstanceId;

            _clipboard.SetRequest(request.Data);
            _clipboard.Share.SetOptions(_termsOptions.ShareLabels, _termsOptions.DefaultShareIndex, _ => UpdateSummary());
            _clipboard.Rank.SetOptions(_termsOptions.RankLabels, _termsOptions.DefaultRankIndex, _ => UpdateSummary());
            _clipboard.Size.SetOptions(_termsOptions.SizeLabels, _termsOptions.DefaultSizeIndex, _ => UpdateSummary());
            UpdateSummary();

            _clipboard.Open(ClipboardSettings, () => _clipboard.Share.Focus());
        }

        private void CloseClipboard()
        {
            if (!_clipboard.IsOpen || _clipboard.IsBusy)
                return;

            _clipboard.Close(ClipboardSettings, OnClipboardClosed);
        }

        // NOTE: The posting flag stops OnRequestRemoved (raised by our own post) from treating it as an expiry.
        private void Post()
        {
            if (!IsClipboardActive || !QuestManager.HasInstance)
                return;

            var request = FindSorted(_clipboardRequestId);
            var terms = CurrentTerms();

            _isPosting = true;
            var posted = request != null && QuestManager.Instance.TryPost(request.InstanceId, terms, out _);
            _isPosting = false;

            if (posted)
                _clipboard.StampAndClose(ClipboardSettings, OnClipboardClosed);
            else
                _clipboard.Close(ClipboardSettings, OnClipboardClosed); // Expired a moment ago, or no free slot.
        }

        private void OnClipboardClosed()
        {
            _clipboardRequestId = -1;
            Refresh();
            Focus((Focusable)_list.ButtonFor(_selectedId) ?? _close);
        }

        private QuestTerms CurrentTerms() =>
            _termsOptions.Terms(_clipboard.Share.Index, _clipboard.Rank.Index, _clipboard.Size.Index);

        private void UpdateSummary()
        {
            var request = FindSorted(_clipboardRequestId);
            if (request == null || _termsOptions == null)
                return;

            var days = QuestCalculator.DurationDays(request.Data, GameConfig.Instance.Quests);
            _clipboard.SetSummary(QuestFormatter.GoldSplit(request.Data, CurrentTerms()), QuestFormatter.Duration(days));
        }

        private bool IsClipboardButtonFocused()
        {
            var focused = _clipboard.Back.focusController?.focusedElement;
            return ReferenceEquals(focused, _clipboard.Back) || ReferenceEquals(focused, _clipboard.Post);
        }

        // ---------- Dismiss ----------

        private void AskDismiss()
        {
            if (!IsPanelActive || _selectedId < 0)
                return;

            var requestId = _selectedId;
            var reputation = GameConfig.Instance.Requests.DismissReputation;
            var confirmed = false;
            _pendingDismissId = requestId;

            _confirm.Show($"Dismiss this request?\n({reputation:+#;-#;0} reputation)",
                          onConfirm: () => confirmed = true,
                          onClosed: () =>
                          {
                              _pendingDismissId = -1;
                              if (confirmed && RequestManager.HasInstance)
                                  RequestManager.Instance.Dismiss(requestId); // No-op if it expired meanwhile.

                              Focus(confirmed ? (Focusable)_list.ButtonFor(_selectedId) ?? _close : _details.Dismiss);
                          });
        }

        // ---------- List ----------

        // NOTE: Sorted by expiry (soonest first). The selection is kept by request; if it left the board, the request now
        // at its old position is selected. Selecting marks it seen (NEW goes away, saved with the request).
        private void Refresh()
        {
            _sorted.Clear();
            if (RequestManager.HasInstance)
                _sorted.AddRange(RequestManager.Instance.Requests);
            _sorted.Sort((a, b) => a.ExpiresAtMinutes != b.ExpiresAtMinutes
                ? a.ExpiresAtMinutes.CompareTo(b.ExpiresAtMinutes)
                : a.InstanceId.CompareTo(b.InstanceId));

            var selected = ResolveSelection();
            if (selected != null && RequestManager.HasInstance)
                RequestManager.Instance.MarkSeen(selected.InstanceId);

            var capacity = GameConfig.Instance.Requests.BoardCapacity;
            _title.Text = $"Requests {_sorted.Count}/{capacity}";
            _list.Show(_sorted, Now, _selectedId, Ranks);

            if (selected == null)
            {
                _details.ShowEmpty("No requests right now.");
                return;
            }

            var posted = QuestManager.HasInstance ? QuestManager.Instance.Quests.Count : 0;
            var open = QuestManager.HasInstance ? QuestManager.Instance.OpenSlots : 0;
            _details.Show(selected, Now, posted, open);
        }

        private ActiveRequest ResolveSelection()
        {
            for (var i = 0; i < _sorted.Count; i++)
            {
                if (_sorted[i].InstanceId != _selectedId)
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
            _selectedId = _sorted[_selectedIndex].InstanceId;
            return _sorted[_selectedIndex];
        }

        private ActiveRequest FindSorted(int instanceId)
        {
            return _sorted.FirstOrDefault(request => request.InstanceId == instanceId);
        }

        private void OnRequestAdded(ActiveRequest _)
        {
            if (IsOpen)
                Refresh();
        }

        // NOTE: If the request being asked about (or on the clipboard) leaves the board, that prompt/clipboard closes.
        private void OnRequestRemoved(ActiveRequest request, RequestEndReason _)
        {
            if (request.InstanceId == _pendingDismissId && _confirm.IsOpen)
                _confirm.Cancel();

            if (request.InstanceId == _clipboardRequestId && _clipboard.IsOpen && !_isPosting && !_clipboard.IsBusy)
                _clipboard.Close(ClipboardSettings, OnClipboardClosed);

            if (IsOpen)
                Refresh();
        }

        // NOTE: Posting or ending a quest, and ranking up, change how many quest-board slots are free ("Accept" + note).
        private void OnQuestBoardChanged(Quest _)
        {
            if (IsOpen)
                Refresh();
        }

        private void OnQuestEnded(Quest quest, QuestEndReason _) => OnQuestBoardChanged(quest);

        private void OnGuildRankUp(GuildRank _)
        {
            if (IsOpen)
                Refresh();
        }

        private void OnHourStarted(GameDateTime _)
        {
            if (IsOpen)
                Refresh();
        }

        private static void Focus(Focusable target) => target?.Focus();
    }
}