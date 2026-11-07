// Alchemy screen: brewing mode (morning prompt) — rows per category × brewable tier with − / +, Brew or Skip; viewing mode
// (click the alchemist) — the potions in stock.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class AlchemyPanel : MenuPanel
    {
        private const string WarningClass = "alc-footer-text--warning";

        private static bool _brewOnNextOpen;

        private readonly List<Button> _buttons = new();
        private readonly List<VisualElement> _navigationOrder = new();
        private readonly List<AlchemyRowView> _rows = new();
        private readonly List<PotionKey> _keys = new();
        private readonly BrewOrder _order = new();
        private QuestCategory[] _categories;
        private OutlinedText _title;
        private ScrollView _list;
        private OutlinedText _empty;
        private OutlinedText _footer;
        private Button _brew;
        private Button _close;
        private OutlinedText _closeLabel;
        private bool _isBrewing;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override Focusable DefaultFocus => _isBrewing ? FirstEnabledPlus() ?? (Focusable)_close : _close;

        // The next time this screen opens, open it in brewing mode (one-shot — the morning prompt).
        public static void PrepareBrewing() => _brewOnNextOpen = true;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>("alc-title");
            _list = documentRoot.Q<ScrollView>("alc-list");
            _empty = documentRoot.Q<OutlinedText>("alc-empty");
            _footer = documentRoot.Q<OutlinedText>("alc-footer");
            _brew = documentRoot.Q<Button>("alc-brew");
            _close = documentRoot.Q<Button>("alc-close");
            _closeLabel = _close?.Q<OutlinedText>();

            // NOTE: Names exactly what's missing (an empty tree usually means UIDocument isn't the first component, or its
            // Source Asset isn't Alchemy.uxml / the UXML failed to import).
            var missing = Missing(("alc-title", _title), ("alc-list", _list), ("alc-empty", _empty), ("alc-footer", _footer),
                ("alc-brew", _brew), ("alc-close", _close), ("alc-close label", _closeLabel));
            if (missing.Length > 0)
            {
                Debug.LogError($"[AlchemyPanel] Missing in the document: {missing} (root has {documentRoot.childCount} children).", this);
                return false;
            }

            // NOTE: One pooled row per category × tier — enough for brewing (all brewable kinds) and viewing (stocked kinds).
            _categories = (QuestCategory[])Enum.GetValues(typeof(QuestCategory));
            var tiers = GameConfig.HasInstance ? GameConfig.Instance.Alchemy.Tiers.Count : 3;
            for (var i = 0; i < _categories.Length * Mathf.Max(1, tiers); i++)
            {
                AlchemyRowView row = new(OnStep);
                row.Minus.RegisterCallback<FocusEvent>(_ => _list.ScrollTo(row.Root));
                row.Plus.RegisterCallback<FocusEvent>(_ => _list.ScrollTo(row.Root));
                _list.Add(row.Root);
                _rows.Add(row);
            }

            _buttons.Clear();
            _navigationOrder.Clear();
            foreach (var row in _rows)
            {
                _buttons.Add(row.Minus);
                _buttons.Add(row.Plus);
                _navigationOrder.Add(row.Minus);
                _navigationOrder.Add(row.Plus);
            }
            _buttons.Add(_brew);
            _buttons.Add(_close);
            _navigationOrder.Add(_brew);
            _navigationOrder.Add(_close);
            return true;
        }

        protected override void OnInitialize()
        {
            _brew.clicked += Brew;
            _close.clicked += Close;
            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsInteractable, null));

            GameEventsRelay.OnPotionStockChanged.AddListener(RefreshIfOpen);
            GameEventsRelay.OnGoldChanged.AddListener(OnGoldChanged);
            GameEventsRelay.OnFacilityCompleted.AddListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityChanged);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnPotionStockChanged.RemoveListener(RefreshIfOpen);
            GameEventsRelay.OnGoldChanged.RemoveListener(OnGoldChanged);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(OnFacilityChanged);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityChanged);
            base.OnDestroy();
        }

        // NOTE: Brewing mode only with an alchemist and free slots; otherwise it falls back to viewing.
        protected override void OnOpening()
        {
            var wantBrew = _brewOnNextOpen;
            _brewOnNextOpen = false;

            _isBrewing = wantBrew && AlchemyManager.HasInstance && AlchemyManager.Instance.FreeSlots > 0;
            _order.Clear();
            _list.scrollOffset = Vector2.zero;
            Refresh();
        }

        // NOTE: Esc = Skip (brewing) / Close (viewing) — the stock stays as it is.
        protected override void OnCancel() => Close();

        private void Close()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        private void OnStep(AlchemyRowView row, int delta)
        {
            if (!IsInteractable || !_isBrewing || row.Key.IsNone || !AlchemyManager.HasInstance)
                return;
            if (_order.Step(row.Key, delta, AlchemyManager.Instance.FreeSlots))
                Refresh();
        }

        // NOTE: Brew re-validates and pays once; on success the screen closes (stock and toast update through events).
        private void Brew()
        {
            if (!IsInteractable || !_isBrewing || !AlchemyManager.HasInstance)
                return;

            if (AlchemyManager.Instance.Brew(_order.ToList()))
                Close();
            else
                Refresh();
        }

        private void Refresh()
        {
            var alchemy = AlchemyManager.HasInstance ? AlchemyManager.Instance : null;
            var capacity = alchemy ? alchemy.Capacity : 0;
            var total = alchemy ? alchemy.Stock.Total : 0;
            var free = alchemy ? alchemy.FreeSlots : 0;

            if (_isBrewing) _order.Clamp(Alchemy.MaxTier, free);

            BuildKeys(alchemy);
            var shown = Mathf.Min(_keys.Count, _rows.Count);
            for (var i = 0; i < _rows.Count; i++)
            {
                if (i >= shown)
                {
                    _rows[i].Hide();
                    continue;
                }

                var key = _keys[i];
                Alchemy.TryGetTier(key.Tier, out var tier);
                var inStock = alchemy ? alchemy.Stock.Count(key) : 0;
                _rows[i].Show(key, $"{tier.Name} {key.Category}", inStock, _order.Count(key), _isBrewing, _order.Total < free);
            }

            _empty.style.display = shown == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _empty.Text = _isBrewing ? "Nothing to brew." : capacity > 0 ? "No potions in stock." : "No alchemist.";
            _title.Text = _isBrewing ? "Morning Brew" : $"Alchemist · {total}/{capacity}";

            _brew.style.display = _isBrewing ? DisplayStyle.Flex : DisplayStyle.None;
            _closeLabel.Text = _isBrewing ? "Skip" : "Close";
            ShowFooter(alchemy, capacity, total, free);
        }

        // NOTE: Brewing: every category × brewable tier. Viewing: only what's in stock (lowest tier first, then category).
        private void BuildKeys(AlchemyManager alchemy)
        {
            _keys.Clear();
            if (_isBrewing)
            {
                var maxTier = Alchemy.MaxTier;
                for (var tier = 1; tier <= maxTier; tier++)
                    foreach (var category in _categories)
                        _keys.Add(new PotionKey(tier, category));
                return;
            }

            if (!alchemy)
                return;
            foreach (var (key, value) in alchemy.Stock.Counts)
            {
                if (value > 0)
                    _keys.Add(key);
            }
            _keys.Sort((a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : a.Category.CompareTo(b.Category));
        }

        private void ShowFooter(AlchemyManager alchemy, int capacity, int total, int free)
        {
            var warn = false;
            string text;

            if (_isBrewing && alchemy)
            {
                var order = _order.ToList();
                var cost = alchemy.CostOf(order);
                var gold = GuildManager.HasInstance ? GuildManager.Instance.Wallet.Gold : 0;
                var affordable = gold >= cost;

                text = $"Free {free - _order.Total}/{capacity} · Cost {cost:N0}g";
                if (!affordable)
                {
                    text = "Not enough gold · " + text;
                    warn = true;
                }
                _brew.SetEnabled(_order.Total > 0 && affordable);
            }
            else
            {
                var hour = GameConfig.Instance.Alchemy.BrewHour;
                text = capacity > 0 ? $"In stock {total}/{capacity} · Next brew {hour:00}:00" : "Build an alchemist to brew potions";
                _brew.SetEnabled(false);
            }

            _footer.Text = text;
            _footer.EnableInClassList(WarningClass, warn);
        }

        private Button FirstEnabledPlus()
        {
            return (from row in _rows where row.Plus.enabledSelf && row.Root.resolvedStyle.display != DisplayStyle.None select row.Plus).FirstOrDefault();
        }

        private void OnGoldChanged(long _, long __) => RefreshIfOpen();
        private void OnFacilityChanged(PlacedFacility _) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen)
                Refresh();
        }
        
        private static string Missing(params (string name, object element)[] parts)
        {
            System.Text.StringBuilder text = new();
            foreach (var (name, element) in parts)
            {
                if (element != null)
                    continue;
                if (text.Length > 0)
                    text.Append(", ");
                text.Append(name);
            }
            return text.ToString();
        }
    }
}