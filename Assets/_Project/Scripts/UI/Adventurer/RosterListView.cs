// The roster panel's list: one pooled row per roster slot, filled in sort order; focused rows scroll into view.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RosterListView
    {
        private readonly ScrollView _scroll;
        private readonly OutlinedText _empty;
        private readonly List<RosterRowView> _rows = new();
        private readonly List<Button> _buttons = new();

        // Every pooled row's button, in list order (for setup and keyboard navigation).
        public IReadOnlyList<Button> Buttons => _buttons;

        public RosterListView(ScrollView scroll, OutlinedText empty, int capacity, Action<Adventurer> onSelected)
        {
            _scroll = scroll;
            _empty = empty;

            for (var i = 0; i < Mathf.Max(1, capacity); i++)
            {
                RosterRowView row = new(clicked =>
                {
                    if (clicked.Adventurer != null)
                        onSelected(clicked.Adventurer);
                });
                row.Button.RegisterCallback<FocusEvent>(_ => _scroll.ScrollTo(row.Button));

                _scroll.Add(row.Button);
                _rows.Add(row);
                _buttons.Add(row.Button);
            }
        }

        /// <param name="adventurers">Already sorted. Anything beyond the pooled rows isn't shown (rows = roster capacity).</param>
        /// <param name="selectedId"></param>
        /// <param name="ranks"></param>
        public void Show(IReadOnlyList<Adventurer> adventurers, int selectedId, GuildRankSettings ranks)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (i < adventurers.Count)
                {
                    var adventurer = adventurers[i];
                    _rows[i].Show(adventurer, ranks.ColorOf(adventurer.Rank), adventurer.Id == selectedId);
                }
                else
                {
                    _rows[i].Hide();
                }
            }

            _empty.style.display = adventurers.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // The visible row showing this adventurer, or null.
        public Button ButtonFor(int adventurerId)
        {
            return (from row in _rows where row.Adventurer != null && row.Adventurer.Id == adventurerId select row.Button).FirstOrDefault();
        }
    }
}