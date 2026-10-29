// The applications list: one pooled row per quest-board slot, filled in sort order; focused rows scroll into view.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ApplicationListView
    {
        private readonly ScrollView _scroll;
        private readonly OutlinedText _empty;
        private readonly List<ApplicationRowView> _rows = new();
        private readonly List<Button> _buttons = new();

        public IReadOnlyList<Button> Buttons => _buttons;

        // NOTE: Every listed quest is on the quest board, so one row per board slot is always enough.
        public ApplicationListView(ScrollView scroll, OutlinedText empty, int rows, Action<Quest> onSelected)
        {
            _scroll = scroll;
            _empty = empty;

            for (var i = 0; i < Mathf.Max(1, rows); i++)
            {
                ApplicationRowView row = new(clicked => { if (clicked.Quest != null) onSelected(clicked.Quest); });
                row.Button.RegisterCallback<FocusEvent>(_ => _scroll.ScrollTo(row.Button));

                _scroll.Add(row.Button);
                _rows.Add(row);
                _buttons.Add(row.Button);
            }
        }

        /// <param name="quests">Already sorted.</param>
        /// <param name="selectedId"></param>
        /// <param name="ranks"></param>
        public void Show(IReadOnlyList<Quest> quests, int selectedId, GuildRankSettings ranks)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (i < quests.Count)
                    _rows[i].Show(quests[i], ranks.ColorOf(quests[i].Request.RecommendedRank), quests[i].Id == selectedId);
                else
                    _rows[i].Hide();
            }

            _empty.style.display = quests.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public Button ButtonFor(int questId)
        {
            return (from row in _rows where row.Quest != null && row.Quest.Id == questId select row.Button).FirstOrDefault();
        }
    }
}