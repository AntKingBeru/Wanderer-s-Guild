// Save-slot list panel: Load Game (load or delete a guild), or New Game's slot picker when every slot is used.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class SaveSlotPanel : MenuPanel
    {
        private const string TitleName = "slots-title";
        private const string ListName = "slots-list";
        private const string DeleteName = "slots-delete";
        private const string BackName = "slots-back";

        [Tooltip("Load: load or delete saves. New Game: pick a slot for a new guild (used slots ask before overwriting).")]
        [SerializeField] private SaveSlotPanelMode mode = SaveSlotPanelMode.Load;

        private readonly List<SaveSlotRow> _rows = new();
        private readonly List<Button> _rowButtons = new();
        private readonly List<Button> _buttons = new();
        private readonly List<VisualElement> _focusOrder = new();
        private OutlinedText _title;
        private ScrollView _list;
        private Button _delete;
        private Button _back;
        private InlineConfirmation _confirm;
        private int _selected = -1;
        private bool _isLeaving; // NOTE: Set once a game launches; blocks every input until the scene unloads.

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override IReadOnlyList<Button> ListButtons => _rowButtons;
        protected override Focusable DefaultFocus => PreferredFocus();

        private bool IsLoadMode => mode == SaveSlotPanelMode.Load;
        private bool IsPanelActive => IsInteractable && !_isLeaving && !_confirm.IsOpen;
        private bool IsConfirmActive => IsInteractable && !_isLeaving && _confirm.IsOpen;
        private static SaveSlotManager Slots => SaveSlotManager.HasInstance ? SaveSlotManager.Instance : null;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>(TitleName);
            _list = documentRoot.Q<ScrollView>(ListName);
            _delete = documentRoot.Q<Button>(DeleteName);
            _back = documentRoot.Q<Button>(BackName);
            _confirm = InlineConfirmation.Bind(documentRoot);

            _rows.Clear();
            _rowButtons.Clear();
            _buttons.Clear();
            if (_title == null || _list == null || _delete == null || _back == null || _confirm == null)
                return false;

            BuildRows();
            _buttons.Add(_delete);
            _buttons.Add(_back);
            _buttons.AddRange(_confirm.Buttons);
            return true;
        }

        protected override void OnInitialize()
        {
            _title.Text = IsLoadMode ? "Load Game" : "Choose a Slot";
            _delete.style.display = IsLoadMode ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (var row in _rows)
            {
                var index = row.Index;
                row.Button.clicked += () => OnRowChosen(index);
                row.Button.RegisterCallback<FocusEvent>(_ => Select(index));
            }

            _delete.clicked += OnDeleteClicked;
            _back.clicked += OnBackClicked;
            _list.RegisterCallback<FocusInEvent>(OnListFocusIn);
            _confirm.Initialize();

            _focusOrder.AddRange(_rowButtons);
            if (IsLoadMode)
                _focusOrder.Add(_delete);
            _focusOrder.Add(_back);

            AddPanelManipulator(new MenuNavigation(_focusOrder, () => IsPanelActive, null, NavigationAxis.Vertical));
            AddPanelManipulator(new MenuNavigation(_confirm.Buttons, () => IsConfirmActive, null, NavigationAxis.Horizontal));

            GameEventsRelay.OnSaveSlotsChanged.AddListener(RefreshRows);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnSaveSlotsChanged.RemoveListener(RefreshRows);
            base.OnDestroy();
        }

        // NOTE: Every open re-reads the slots from disk (files may have changed) and starts with nothing selected.
        protected override void OnOpening()
        {
            _confirm.HideImmediate();
            _isLeaving = false;
            _selected = -1;
            _list.scrollOffset = Vector2.zero;

            if (Slots)
                Slots.Refresh(); // Raises OnSaveSlotsChanged → RefreshRows.
            else
                RefreshRows();
        }

        // NOTE: Esc answers an open confirmation (No) first, then closes the panel.
        protected override void OnCancel()
        {
            if (_isLeaving)
                return;

            if (_confirm.IsOpen)
                _confirm.Cancel();
            else
                Host.Close(Id);
        }

        private void BuildRows()
        {
            _list.Clear();

            var count = GameConfig.Instance.SaveSlots.SlotCount;
            for (var i = 0; i < count; i++)
            {
                SaveSlotRow row = new(i);
                _rows.Add(row);
                _rowButtons.Add(row.Button);
                _list.Add(row.Button);
            }
        }

        private void RefreshRows()
        {
            var slots = Slots;
            var format = GameConfig.Instance.SaveSlots.SavedAtFormat;

            foreach (var row in _rows)
            {
                var info = slots ? slots.GetSlot(row.Index) : new SaveSlotInfo(row.Index, SaveSlotState.Empty, null);
                var thumbnail = slots ? slots.GetThumbnail(row.Index) : null;

                row.Bind(info, thumbnail, format);
                row.Button.SetEnabled(IsSelectable(info)); // Disabled rows are greyed and skipped by navigation.
                row.SetSelected(row.Index == _selected);
            }

            UpdateDeleteState();
        }

        // NOTE: Load mode: any used slot (unreadable ones too, so they can be deleted). New Game mode: every slot.
        private bool IsSelectable(SaveSlotInfo info) => !IsLoadMode || info.State != SaveSlotState.Empty;

        private void Select(int index)
        {
            _selected = index;
            foreach (var row in _rows)
                row.SetSelected(row.Index == index);

            UpdateDeleteState();
        }

        private void UpdateDeleteState() =>
            _delete.SetEnabled(IsLoadMode && TryGetSelected(out var info) && info.State != SaveSlotState.Empty);

        private bool TryGetSelected(out SaveSlotInfo info)
        {
            info = default;
            if (!Slots || _selected < 0)
                return false;

            info = Slots.GetSlot(_selected);
            return true;
        }

        private void OnRowChosen(int index)
        {
            if (!IsPanelActive || !Slots)
                return;

            var info = Slots.GetSlot(index);
            if (IsLoadMode)
            {
                if (info.State == SaveSlotState.Ready)
                    Launch(index);
                else if (info.State == SaveSlotState.Corrupted)
                    AskDelete(info, "This save can't be read.\nDelete it?", _rows[index].Button);
                return;
            }

            if (info.State == SaveSlotState.Empty)
                Launch(index);
            else
                AskOverwrite(info);
        }

        // NOTE: No is pre-selected. The old save is only replaced when the new guild first saves.
        private void AskOverwrite(SaveSlotInfo info)
        {
            var index = info.Index;
            var confirmed = false;

            _confirm.Show($"Overwrite this save?\n{Describe(info)}",
                          onConfirm: () => confirmed = true,
                          onClosed: () =>
                          {
                              if (confirmed && Launch(index))
                                  return;
                              FocusRow(index);
                          });
        }

        private void OnDeleteClicked()
        {
            if (!IsPanelActive || !IsLoadMode || !TryGetSelected(out var info) || info.State == SaveSlotState.Empty)
                return;

            AskDelete(info, $"Delete this save?\n{Describe(info)}", _delete);
        }

        /// <param name="info"></param>
        /// <param name="message"></param>
        /// <param name="returnFocus">Focused again if the player answers No.</param>
        private void AskDelete(SaveSlotInfo info, string message, Focusable returnFocus)
        {
            var index = info.Index;
            var deleted = false;

            _confirm.Show(message,
                          onConfirm: () => deleted = Slots && Slots.Delete(index),
                          onClosed: () =>
                          {
                              if (!deleted)
                              {
                                  returnFocus.Focus();
                                  return;
                              }

                              // NOTE: The deleted row is now disabled — move focus somewhere usable explicitly.
                              _selected = -1;
                              RefreshRows();
                              PreferredFocus().Focus();
                          });
        }

        private void OnBackClicked()
        {
            if (IsPanelActive)
                Host.Close(Id);
        }

        private bool Launch(int index)
        {
            var started = IsLoadMode ? GameSessionLauncher.LoadSlot(index) : GameSessionLauncher.StartNewGame(index);
            if (started)
                _isLeaving = true;
            return started;
        }

        // NOTE: Load mode prefers a loadable save (most recent, then first) over an unreadable one; then any usable
        // row; Back as a last resort.
        private Focusable PreferredFocus()
        {
            if (IsLoadMode && Slots)
            {
                if (Slots.TryGetMostRecent(out var recent) && recent < _rows.Count && _rows[recent].Button.enabledInHierarchy)
                    return _rows[recent].Button;

                foreach (var row in _rows.Where(row => row.Button.enabledInHierarchy && Slots.GetSlot(row.Index).State == SaveSlotState.Ready))
                {
                    return row.Button;
                }
            }

            foreach (var row in _rows.Where(row => row.Button.enabledInHierarchy))
            {
                return row.Button;
            }
            return _back;
        }

        private void FocusRow(int index)
        {
            if (index >= 0 && index < _rows.Count && _rows[index].Button.enabledInHierarchy)
                _rows[index].Button.Focus();
            else
                PreferredFocus().Focus();
        }

        // NOTE: Keeps the focused row visible inside the scrolling list.
        private void OnListFocusIn(FocusInEvent evt)
        {
            if (evt.target is VisualElement element && element != _list)
                _list.ScrollTo(element);
        }

        private static string Describe(SaveSlotInfo info) =>
            info.State == SaveSlotState.Ready ? info.Header.GuildName : "Unreadable save";
    }
}