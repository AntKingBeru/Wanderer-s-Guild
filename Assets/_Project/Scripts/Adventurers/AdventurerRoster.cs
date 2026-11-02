// The guild's adventurers; capacity comes from a provider (the facilities), read on demand. Pure state — no events.
using System;
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class AdventurerRoster
    {
        private readonly List<Adventurer> _adventurers = new();
        private readonly HashSet<string> _names = new();
        private readonly Func<int> _capacity;

        public AdventurerRoster(Func<int> capacity) => _capacity = capacity;

        public IReadOnlyList<Adventurer> Adventurers => _adventurers;
        public int Capacity => Math.Max(1, _capacity());
        public bool IsFull => _adventurers.Count >= Capacity;

        // Names in use (for unique naming).
        public ICollection<string> Names => _names;

        // New members: false when full or the id is already in the roster.
        public bool TryAdd(Adventurer adventurer)
        {
            if (adventurer == null || IsFull || Find(adventurer.Id) != null)
                return false;

            Insert(adventurer);
            return true;
        }

        // Loading a save: ignores capacity (a data change may have lowered it) — nobody is dropped.
        public bool AddRestored(Adventurer adventurer)
        {
            if (adventurer == null || Find(adventurer.Id) != null)
                return false;

            Insert(adventurer);
            return true;
        }

        public Adventurer Find(int id)
        {
            return _adventurers.FirstOrDefault(adventurer => adventurer.Id == id);
        }

        private void Insert(Adventurer adventurer)
        {
            _adventurers.Add(adventurer);
            _names.Add(adventurer.Name);
        }
    }
}