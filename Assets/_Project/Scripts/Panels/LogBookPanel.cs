// The office log book screen: dated entries for the last N days, newest or oldest first; Left/Right buttons, Up/Down scrolls.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: A virtualized ListView (only the visible rows exist), fixed 10px rows. Opened from the Recruits screen; closing
    // returns there (the panel host's stack).
    public class LogBookPanel : MenuPanel
    {
        private const float RowHeight = 10f;
        private const float ScrollStep = RowHeight * 3f;
        private const string NewestLabel = "Newest first";
        private const string OldestLabel = "Oldest first";

        private readonly List<Button> _buttons = new();
        private readonly List<VisualElement> _navigationOrder = new();
        private readonly List<LogLine> _lines = new();
        private OutlinedText _title;
        private ListView _list;
        private ScrollView _scroll;
        private OutlinedText _empty;
        private Button _order;
        private OutlinedText _orderLabel;
        private Button _close;
        private bool _newestFirst = true;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override Focusable DefaultFocus => _close;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>("log-title");
            _list = documentRoot.Q<ListView>("log-list");
            _empty = documentRoot.Q<OutlinedText>("log-empty");
            _order = documentRoot.Q<Button>("log-order");
            _close = documentRoot.Q<Button>("log-close");
            _orderLabel = _order?.Q<OutlinedText>();
            _scroll = _list?.Q<ScrollView>();

            if (_title == null || _list == null || _empty == null || _order == null || _close == null || _orderLabel == null ||
                _scroll == null)
                return false;

            // NOTE: Read-only rows: no selection, never focused (the buttons keep the focus).
            _list.fixedItemHeight = RowHeight;
            _list.selectionType = SelectionType.None;
            _list.focusable = false;
            _list.makeItem = LogRowView.Create;
            _list.bindItem = (row, index) => LogRowView.Bind(row, _lines[index]);
            _list.itemsSource = _lines;

            _buttons.Clear();
            _buttons.Add(_order);
            _buttons.Add(_close);

            _navigationOrder.Clear();
            _navigationOrder.Add(_order);
            _navigationOrder.Add(_close);
            return true;
        }

        protected override void OnInitialize()
        {
            _order.clicked += ToggleOrder;
            _close.clicked += Close;

            // NOTE: Left/Right walk the buttons; Up/Down pass through to OnMove, which scrolls the log.
            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsInteractable, null, NavigationAxis.Horizontal,
                                                   passThroughCrossAxis: true));
            Root.RegisterCallback<NavigationMoveEvent>(OnMove);
            GameEventsRelay.OnLogEntryAdded.AddListener(OnEntryAdded);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnLogEntryAdded.RemoveListener(OnEntryAdded);
            if (Root != null)
                Root.UnregisterCallback<NavigationMoveEvent>(OnMove);
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            Refresh();
            _scroll.scrollOffset = Vector2.zero;
        }

        protected override void OnCancel() => Close();

        private void Close()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        private void Refresh()
        {
            var log = GuildLog.HasInstance ? GuildLog.Instance : null;
            if (log != null)
                LogBookLayout.Build(log.Entries, _newestFirst, _lines);
            else
                _lines.Clear();

            _list.RefreshItems();
            _title.Text = log != null ? $"Log book · last {log.RetentionDays} days" : "Log book";
            _empty.style.display = _lines.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _orderLabel.Text = _newestFirst ? NewestLabel : OldestLabel;
        }

        private void ToggleOrder()
        {
            if (!IsInteractable)
                return;

            _newestFirst = !_newestFirst;
            Refresh();
            _scroll.scrollOffset = Vector2.zero;
        }

        private void OnEntryAdded(LogEntry _)
        {
            if (IsOpen)
                Refresh();
        }

        // NOTE: Up/Down scroll three rows; consumed so the default focus ring doesn't move.
        private void OnMove(NavigationMoveEvent evt)
        {
            if (!IsInteractable)
                return;

            var step = evt.direction switch
            {
                NavigationMoveEvent.Direction.Up => -ScrollStep,
                NavigationMoveEvent.Direction.Down => ScrollStep,
                _ => 0f
            };
            if (step == 0f)
                return;

            _scroll.scrollOffset = new Vector2(0f, Mathf.Max(0f, _scroll.scrollOffset.y + step));
            evt.StopPropagation();
            Root.focusController?.IgnoreEvent(evt);
        }
    }
}