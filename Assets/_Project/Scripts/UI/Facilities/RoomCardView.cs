// The room info card in build mode (above the build bar): title, state, effects, next level, reasons, Upgrade / Demolish / close.
using System;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RoomCardView
    {
        private readonly VisualElement _root;
        private readonly OutlinedText _title;
        private readonly OutlinedText _state;
        private readonly OutlinedText _effects;
        private readonly OutlinedText _next;
        private readonly OutlinedText _reasons;
        private readonly Button _upgrade;
        private readonly OutlinedText _upgradeLabel;
        private readonly Button _demolish;
        private readonly OutlinedText _demolishLabel;
        private readonly MarqueeLine[] _marquees;

        private RoomCardView(VisualElement root, OutlinedText title, OutlinedText state, OutlinedText effects, OutlinedText next,
                             OutlinedText reasons, Button upgrade, OutlinedText upgradeLabel, Button demolish,
                             OutlinedText demolishLabel, Button close, Action onUpgrade, Action onDemolish, Action onClose)
        {
            _root = root;
            _title = title;
            _state = state;
            _effects = effects;
            _next = next;
            _reasons = reasons;
            _upgrade = upgrade;
            _upgradeLabel = upgradeLabel;
            _demolish = demolish;
            _demolishLabel = demolishLabel;

            // NOTE: Never focusable — Space is UI Submit and the time-pause hotkey.
            foreach (var button in new[] { upgrade, demolish, close })
                button.focusable = false;

            upgrade.clicked += onUpgrade;
            demolish.clicked += onDemolish;
            close.clicked += onClose;
            _marquees = new[] { new MarqueeLine(title), new MarqueeLine(state), new MarqueeLine(effects),
                new MarqueeLine(next), new MarqueeLine(reasons) };
            Hide();
        }

        public static RoomCardView Bind(VisualElement documentRoot, Action onUpgrade, Action onDemolish, Action onClose)
        {
            var root = documentRoot?.Q<VisualElement>("room-card");
            var title = documentRoot?.Q<OutlinedText>("room-card-title");
            var state = documentRoot?.Q<OutlinedText>("room-card-state");
            var effects = documentRoot?.Q<OutlinedText>("room-card-effects");
            var next = documentRoot?.Q<OutlinedText>("room-card-next");
            var reasons = documentRoot?.Q<OutlinedText>("room-card-reasons");
            var upgrade = documentRoot?.Q<Button>("room-card-upgrade");
            var demolish = documentRoot?.Q<Button>("room-card-demolish");
            var close = documentRoot?.Q<Button>("room-card-close");
            var upgradeLabel = upgrade?.Q<OutlinedText>();
            var demolishLabel = demolish?.Q<OutlinedText>();

            var complete = root != null && title != null && state != null && effects != null && next != null && reasons != null &&
                            upgrade != null && demolish != null && close != null && upgradeLabel != null && demolishLabel != null;
            return complete
                ? new RoomCardView(root, title, state, effects, next, reasons, upgrade, upgradeLabel, demolish, demolishLabel,
                                   close, onUpgrade, onDemolish, onClose)
                : null;
        }

        // NOTE: "Under construction" / "Highest level" already explain a disabled Upgrade, so only other reasons are listed.
        public void Show(PlacedFacility room, FacilityCheck upgrade, FacilityCheck demolish, long nowMinutes)
        {
            _title.Text = FacilityFormatter.Title(room);
            _state.Text = FacilityFormatter.State(room, nowMinutes);
            SetLine(_effects, FacilityFormatter.Effects(room));
            SetLine(_next, FacilityFormatter.Next(room));

            var upgradeExplained = room.IsUnderConstruction || room.Level >= room.Data.MaxLevel;
            var reasons = string.Empty;
            if (!upgrade.Allowed && !upgradeExplained)
                reasons += $"Upgrade: {upgrade.Reason}";
            if (!demolish.Allowed)
                reasons += (reasons.Length > 0 ? "\n" : string.Empty) + $"Demolish: {demolish.Reason}";
            SetLine(_reasons, reasons);

            _upgradeLabel.Text = FacilityFormatter.UpgradeLabel(room);
            _upgrade.SetEnabled(upgrade.Allowed);
            _demolishLabel.Text = FacilityFormatter.DemolishLabel(demolish);
            _demolish.SetEnabled(demolish.Allowed);

            _root.style.display = DisplayStyle.Flex;
            
            foreach (var line in _marquees)
                line.Refresh();
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
            if (_marquees == null)
                return;
            foreach (var line in _marquees)
                line.Stop();
        }

        private static void SetLine(OutlinedText line, string text)
        {
            line.Text = text;
            line.style.display = text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}