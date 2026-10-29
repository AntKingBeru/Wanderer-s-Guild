// The guild's adventurers (capacity-limited); pure state — AdventurerManager raises the events.
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class AdventurerRoster
    {
        private readonly List<Adventurer> _adventurers = new();
        private readonly HashSet<string> _names = new();
        private readonly int _capacity;

        public AdventurerRoster(int capacity) => _capacity = System.Math.Max(1, capacity);

        public IReadOnlyList<Adventurer> Adventurers => _adventurers;
        public int Capacity => _capacity;
        public bool IsFull => _adventurers.Count >= _capacity;

        // Names in use (for unique naming).
        public ICollection<string> Names => _names;

        // False when full or the id is already in the roster.
        public bool TryAdd(Adventurer adventurer)
        {
            if (adventurer == null || IsFull || Find(adventurer.Id) != null) return false;

            _adventurers.Add(adventurer);
            _names.Add(adventurer.Name);
            return true;
        }

        public Adventurer Find(int id)
        {
            return _adventurers.FirstOrDefault(adventurer => adventurer.Id == id);
        }
    }
}