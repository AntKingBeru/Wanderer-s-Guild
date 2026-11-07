// The floating adventurer card in the world view: name, species · class · level · rank, status; Details and close buttons.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class AdventurerCardView
    {
        private readonly VisualElement _root;
        private readonly OutlinedText _name;
        private readonly OutlinedText _info;
        private readonly OutlinedText _status;

        public VisualElement Root => _root;

        private AdventurerCardView(VisualElement root, OutlinedText name, OutlinedText info, OutlinedText status,
                                   Button details, Button close, Action onDetails, Action onClose)
        {
            _root = root;
            _name = name;
            _info = info;
            _status = status;

            // NOTE: Never focusable — Space is UI Submit and the time-pause hotkey.
            details.focusable = false;
            close.focusable = false;
            details.clicked += onDetails;
            close.clicked += onClose;
            Hide();
        }

        public static AdventurerCardView Bind(VisualElement documentRoot, Action onDetails, Action onClose)
        {
            var root = documentRoot?.Q<VisualElement>("world-card");
            var name = documentRoot?.Q<OutlinedText>("world-card-name");
            var info = documentRoot?.Q<OutlinedText>("world-card-info");
            var status = documentRoot?.Q<OutlinedText>("world-card-status");
            var details = documentRoot?.Q<Button>("world-card-details");
            var close = documentRoot?.Q<Button>("world-card-close");

            return root != null && name != null && info != null && status != null && details != null && close != null
                ? new AdventurerCardView(root, name, info, status, details, close, onDetails, onClose)
                : null;
        }

        public bool IsVisible => _root.resolvedStyle.display != DisplayStyle.None;

        // "Bran" / "Elf · Archer · Lv 2 · Rank F" / "Idle · Well fed (+6%)".
        public void SetContent(Adventurer adventurer, long nowMinutes)
        {
            _name.Text = adventurer.Name;
            _info.Text = $"{AdventurerFormatter.SpeciesAndClass(adventurer)} · {AdventurerFormatter.ShortLevel(adventurer)} · Rank {adventurer.Rank}";
            _status.Text = AdventurerFormatter.Status(adventurer, nowMinutes).Replace("Status: ", string.Empty);
        }

        // Places the card's top-left at a local position, clamped inside the area (whole pixels).
        // NOTE: Before the first layout the size is unknown (NaN) — it's placed unclamped and corrected next frame.
        public void Place(Vector2 localPosition, Rect area, float topMargin)
        {
            float width = _root.resolvedStyle.width, height = _root.resolvedStyle.height;
            var p = localPosition;
            if (!float.IsNaN(width) && !float.IsNaN(height))
            {
                p.x = Mathf.Clamp(p.x, area.xMin, Mathf.Max(area.xMin, area.xMax - width));
                p.y = Mathf.Clamp(p.y, area.yMin + topMargin, Mathf.Max(area.yMin + topMargin, area.yMax - height));
            }

            _root.style.left = Mathf.Round(p.x);
            _root.style.top = Mathf.Round(p.y);
        }

        public void Show() => _root.style.display = DisplayStyle.Flex;
        public void Hide() => _root.style.display = DisplayStyle.None;
    }
}