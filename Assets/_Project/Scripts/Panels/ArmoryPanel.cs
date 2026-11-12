// Armory screen (click the armory): items for sale with units left + details, and the next restocking's time and cost.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class ArmoryPanel : MenuPanel
    {
        private const string EmptyMessage = "Nothing for sale yet.";
        private const int RowPool = 24; // Extra items aren't listed (warned once).

        private readonly List<Button> _buttons = new();
        private readonly List<Button> _rowButtons = new();
        private readonly List<VisualElement> _navigationOrder = new();
        private readonly List<ArmoryRowView> _rows = new();
        private readonly List<EquipmentData> _sorted = new();
        private Dictionary<string, ClassData> _classes = new();
        private OutlinedText _title;
        private ScrollView _list;
        private OutlinedText _listEmpty;
        private OutlinedText _footer;
        private ArmoryDetailsView _details;
        private Button _close;
        private string _selectedId;
        private int _selectedIndex;
        private bool _warnedPool;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override IReadOnlyList<Button> ListButtons => _rowButtons;
        protected override Focusable DefaultFocus => (Focusable)ButtonFor(_selectedId) ?? _close;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>("arm-title");
            _list = documentRoot.Q<ScrollView>("arm-list");
            _listEmpty = documentRoot.Q<OutlinedText>("arm-list-empty");
            _footer = documentRoot.Q<OutlinedText>("arm-footer");
            _close = documentRoot.Q<Button>("arm-close");
            _details = ArmoryDetailsView.Bind(documentRoot);

            if (_title == null || _list == null || _listEmpty == null || _footer == null || _close == null || _details == null)
                return false;

            for (var i = 0; i < RowPool; i++)
            {
                ArmoryRowView row = new(clicked => Select(clicked.Item));
                row.Button.RegisterCallback<FocusEvent>(_ => _list.ScrollTo(row.Button));
                _list.Add(row.Button);
                _rows.Add(row);
                _rowButtons.Add(row.Button);
            }

            _buttons.Clear();
            _buttons.Add(_close);

            _navigationOrder.Clear();
            _navigationOrder.AddRange(_rowButtons);
            _navigationOrder.Add(_close);
            return true;
        }

        protected override void OnInitialize()
        {
            _close.clicked += Close;
            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsInteractable, null));

            var classes = GameConfig.Instance.Adventurers.ClassDatabase;
            if (classes != null)
                _classes = classes.BuildLookup();

            GameEventsRelay.OnArmoryStockChanged.AddListener(RefreshIfOpen);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnGuildRankUp.AddListener(OnRankUp);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnArmoryStockChanged.RemoveListener(RefreshIfOpen);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnGuildRankUp.RemoveListener(OnRankUp);
            base.OnDestroy();
        }

        protected override void OnOpening() => Refresh();

        protected override void OnCancel() => Close();

        private void Close()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        private void Select(EquipmentData item)
        {
            if (!IsInteractable || item == null)
                return;

            _selectedId = item.Id;
            Refresh();
        }

        // NOTE: By slot, then armory tier, then price. Selection kept by item id; rows past the pool aren't listed.
        private void Refresh()
        {
            _sorted.Clear();
            var tier = 0;
            long fullCost = 0;
            ArmoryStock stock = null;

            if (ArmoryManager.HasInstance)
            {
                var armory = ArmoryManager.Instance;
                tier = armory.StockTier;
                stock = armory.Stock;
                _sorted.AddRange(armory.OnSale);
                fullCost = armory.FullRestockCost;
            }
            _sorted.Sort(CompareItems);

            if (_sorted.Count > RowPool && !_warnedPool)
            {
                _warnedPool = true;
                Debug.LogWarning($"[ArmoryPanel] {_sorted.Count} items but only {RowPool} rows; raise RowPool.", this);
            }

            var selected = ResolveSelection();
            _title.Text = ArmoryFormatter.Title(tier);

            for (var i = 0; i < _rows.Count; i++)
            {
                if (i < _sorted.Count)
                {
                    if (stock != null)
                        _rows[i].Show(_sorted[i], stock.Units(_sorted[i]), _sorted[i].Id == _selectedId);
                }
                else _rows[i].Hide();
            }
            _listEmpty.style.display = _sorted.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (selected != null)
            {
                if (stock != null)
                    _details.Show(selected, stock.Units(selected), _classes);
            }
            else _details.ShowEmpty(EmptyMessage);

            var dawn = GameConfig.Instance.Time.DawnHour;
            _footer.Text = tier > 0 ? ArmoryFormatter.Footer(fullCost, MinutesUntilDawn(dawn), dawn) : "No armory";
        }

        private static long MinutesUntilDawn(int dawnHour)
        {
            const long day = GameCalendar.MinutesPerDay;
            var ofDay = RequestManager.NowMinutes % day;
            var dawn = dawnHour * 60L;
            return ofDay < dawn ? dawn - ofDay : day - ofDay + dawn;
        }

        private static int CompareItems(EquipmentData a, EquipmentData b)
        {
            if (a.Slot != b.Slot)
                return a.Slot.CompareTo(b.Slot);
            if (a.ArmoryLevel != b.ArmoryLevel)
                return a.ArmoryLevel.CompareTo(b.ArmoryLevel);
            return a.Price != b.Price ? a.Price.CompareTo(b.Price) : string.CompareOrdinal(a.Id, b.Id);
        }

        private EquipmentData ResolveSelection()
        {
            var visible = Mathf.Min(_sorted.Count, RowPool);
            for (var i = 0; i < visible; i++)
            {
                if (_sorted[i].Id != _selectedId)
                    continue;

                _selectedIndex = i;
                return _sorted[i];
            }

            if (visible == 0)
            {
                _selectedId = null;
                _selectedIndex = 0;
                return null;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, visible - 1);
            _selectedId = _sorted[_selectedIndex].Id;
            return _sorted[_selectedIndex];
        }

        private Button ButtonFor(string itemId)
        {
            return string.IsNullOrEmpty(itemId)
                ? null
                : (from row in _rows where row.Item != null && row.Item.Id == itemId select row.Button).FirstOrDefault();
        }

        private void OnHourStarted(GameDateTime _) => RefreshIfOpen();
        private void OnRankUp(GuildRank _) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen) Refresh();
        }
    }
}