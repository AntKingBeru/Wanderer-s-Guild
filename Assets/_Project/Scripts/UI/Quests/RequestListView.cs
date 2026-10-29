// The request board's list: one pooled row per board slot, filled in sort order; focused rows scroll into view.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RequestListView
    {
        private readonly ScrollView _scroll;
        private readonly OutlinedText _empty;
        private readonly List<RequestRowView> _rows = new();
        private readonly List<Button> _buttons = new();

        // Every pooled row's button, in list order (for setup and keyboard navigation).
        public IReadOnlyList<Button> Buttons => _buttons;

        public RequestListView(ScrollView scroll, OutlinedText empty, int capacity, Action<ActiveRequest> onSelected)
        {
            _scroll = scroll;
            _empty = empty;

            for (var i = 0; i < Mathf.Max(1, capacity); i++)
            {
                RequestRowView row = new(clicked => { if (clicked.Request != null) onSelected(clicked.Request); });
                row.Button.RegisterCallback<FocusEvent>(_ => _scroll.ScrollTo(row.Button));

                _scroll.Add(row.Button);
                _rows.Add(row);
                _buttons.Add(row.Button);
            }
        }

        /// <param name="requests">Already sorted. Anything beyond the pooled rows isn't shown (rows = board capacity).</param>
        /// <param name="nowMinutes"></param>
        /// <param name="selectedId"></param>
        /// <param name="ranks"></param>
        public void Show(IReadOnlyList<ActiveRequest> requests, long nowMinutes, int selectedId, GuildRankSettings ranks)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (i < requests.Count)
                {
                    var request = requests[i];
                    _rows[i].Show(request, nowMinutes, ranks.ColorOf(request.Data.RecommendedRank), request.InstanceId == selectedId);
                }
                else
                {
                    _rows[i].Hide();
                }
            }

            _empty.style.display = requests.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // The visible row showing this request, or null.
        public Button ButtonFor(int instanceId)
        {
            return (from row in _rows where row.Request != null && row.Request.InstanceId == instanceId select row.Button).FirstOrDefault();
        }
    }
}