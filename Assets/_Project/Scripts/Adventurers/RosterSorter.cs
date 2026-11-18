// Orders the roster by the chosen key (rank, level, class, status, name, mood); ties fall back to rank, level, name, then id.
using System;
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Strategy — one cached Comparison per RosterSort (indexed by the enum value), so sorting allocates nothing. Mood is
    // computed once per sort into a reused dictionary (a live score is too costly to recompute inside a comparison).
    public sealed class RosterSorter
    {
        private readonly Dictionary<int, float> _mood = new();
        private readonly Comparison<Adventurer>[] _comparisons;

        public RosterSorter()
        {
            _comparisons = new Comparison<Adventurer>[] { ByRank, ByLevel, ByClass, ByStatus, ByName, ByMood };
        }

        public void Sort(List<Adventurer> adventurers, RosterSort sort)
        {
            var index = (int)sort;
            if (index < 0 || index >= _comparisons.Length)
                index = 0;

            if (sort == RosterSort.Mood)
                CacheMoods(adventurers);
            adventurers.Sort(_comparisons[index]);
        }

        public static string Label(RosterSort sort) => sort switch
        {
            RosterSort.Level => "Level",
            RosterSort.Class => "Class",
            RosterSort.Status => "Status",
            RosterSort.Name => "Name",
            RosterSort.Mood => "Mood",
            _ => "Rank"
        };

        private int ByRank(Adventurer a, Adventurer b) => Fallback(a, b);

        private int ByLevel(Adventurer a, Adventurer b) =>
            a.Level != b.Level ? b.Level.CompareTo(a.Level) : Fallback(a, b);

        private int ByClass(Adventurer a, Adventurer b)
        {
            var byClass = string.Compare(ClassName(a), ClassName(b), StringComparison.OrdinalIgnoreCase);
            return byClass != 0 ? byClass : Fallback(a, b);
        }

        private int ByStatus(Adventurer a, Adventurer b)
        {
            var byStatus = StatusOrder(a.Status).CompareTo(StatusOrder(b.Status));
            return byStatus != 0 ? byStatus : Fallback(a, b);
        }

        private int ByName(Adventurer a, Adventurer b)
        {
            var byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return byName != 0 ? byName : Fallback(a, b);
        }

        // NOTE: Unhappiest first — the ones who need attention. Without the satisfaction system every mood is equal.
        private int ByMood(Adventurer a, Adventurer b)
        {
            var byMood = MoodOf(a).CompareTo(MoodOf(b));
            return byMood != 0 ? byMood : Fallback(a, b);
        }

        // Rank (highest first), level (highest first), name, then id — a total order, so rows never swap between refreshes.
        private static int Fallback(Adventurer a, Adventurer b)
        {
            if (a.Rank != b.Rank)
                return b.Rank.CompareTo(a.Rank);
            if (a.Level != b.Level)
                return b.Level.CompareTo(a.Level);

            var byName = string.CompareOrdinal(a.Name, b.Name);
            return byName != 0 ? byName : a.Id.CompareTo(b.Id);
        }

        // NOTE: At the guild first (idle, signed up, injured), then on the way back, then away.
        private static int StatusOrder(AdventurerStatus status) => status switch
        {
            AdventurerStatus.Idle => 0,
            AdventurerStatus.SignedUp => 1,
            AdventurerStatus.Injured => 2,
            AdventurerStatus.Returning => 3,
            AdventurerStatus.OnQuest => 4,
            AdventurerStatus.InExam => 5,
            AdventurerStatus.InTrial => 6,
            _ => 7
        };

        private static string ClassName(Adventurer adventurer) =>
            adventurer.Class != null ? adventurer.Class.DisplayName : string.Empty;

        private void CacheMoods(List<Adventurer> adventurers)
        {
            _mood.Clear();
            if (!SatisfactionManager.HasInstance)
                return;

            var satisfaction = SatisfactionManager.Instance;
            foreach (var adventurer in adventurers)
                _mood[adventurer.Id] = satisfaction.Score(adventurer);
        }

        private float MoodOf(Adventurer adventurer) => _mood.GetValueOrDefault(adventurer.Id, 0f);
    }
}