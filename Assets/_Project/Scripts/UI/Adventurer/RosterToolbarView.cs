// The roster's Sort (one choice) and Filter (several) dropdowns in the title row; owns the filter flags and the field labels.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RosterToolbarView
    {
        private const string SortPrefix = "Sort: ";
        private const string FilterPrefix = "Filter: ";

        // NOTE: The filter list's items in order; item 0 is "All" (no flags).
        private static readonly RosterFilter[] FilterItems =
        {
            RosterFilter.None, RosterFilter.Idle, RosterFilter.Busy, RosterFilter.Injured, RosterFilter.Ready
        };

        private RosterFilter _filter;
        private Action<RosterFilter> _onFilter;

        public PixelDropdown Sort { get; }
        public PixelChecklistDropdown Filter { get; }
        public bool IsOpen => Sort.IsOpen || Filter.IsOpen;

        private RosterToolbarView(PixelDropdown sort, PixelChecklistDropdown filter)
        {
            Sort = sort;
            Filter = filter;
        }

        public static RosterToolbarView Bind(VisualElement root)
        {
            var sort = root?.Q<PixelDropdown>("roster-sort");
            var filter = root?.Q<PixelChecklistDropdown>("roster-filter");
            return sort != null && filter != null ? new RosterToolbarView(sort, filter) : null;
        }

        // NOTE: Call before handing Sort.Items / Filter.Items to MenuNavigation (the lists are created here).
        public void Initialize(VisualElement layer, GameObject linkTarget, RosterSort sort, RosterFilter filter,
                               Action<RosterSort> onSort, Action<RosterFilter> onFilter)
        {
            _filter = filter;
            _onFilter = onFilter;

            Sort.Initialize(layer, linkTarget);
            Filter.Initialize(layer, linkTarget);

            Sort.Prefix = SortPrefix;
            Sort.SetOptions(SortLabels(), (int)sort, index => onSort((RosterSort)index));
            Filter.SetOptions(FilterLabels(), IsChecked, Toggle);
            UpdateFilterLabel();
        }

        public void CloseAll(bool refocus)
        {
            Sort.Close(refocus);
            Filter.Close(refocus);
        }

        private bool IsChecked(int index) =>
            index == 0 ? _filter == RosterFilter.None : (_filter & FilterItems[index]) != RosterFilter.None;

        // NOTE: "All" clears every flag; any other item flips its own flag (unticking the last one is "All" again).
        private void Toggle(int index)
        {
            _filter = index == 0 ? RosterFilter.None : _filter ^ FilterItems[index];
            UpdateFilterLabel();
            _onFilter?.Invoke(_filter);
        }

        // "Filter: All", the single ticked item, or how many are ticked.
        private void UpdateFilterLabel()
        {
            var ticked = 0;
            var last = RosterFilter.None;
            for (var i = 1; i < FilterItems.Length; i++)
            {
                if ((_filter & FilterItems[i]) == RosterFilter.None)
                    continue;
                ticked++;
                last = FilterItems[i];
            }

            var summary = ticked switch
            {
                0 => RosterFilterRules.Label(RosterFilter.None),
                1 => RosterFilterRules.Label(last),
                _ => ticked.ToString()
            };
            Filter.SetLabel(FilterPrefix + summary);
        }

        private static List<string> SortLabels()
        {
            return (from RosterSort sort in Enum.GetValues(typeof(RosterSort)) select RosterSorter.Label(sort)).ToList();
        }

        private static List<string> FilterLabels()
        {
            return FilterItems.Select(RosterFilterRules.Label).ToList();
        }
    }
}