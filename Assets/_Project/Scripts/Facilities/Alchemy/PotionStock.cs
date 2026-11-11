// The alchemist's potions (pure state): count per tier + category, total, take one, add, clear, save/restore.
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class PotionStock
    {
        private readonly Dictionary<PotionKey, int> _counts = new();

        public int Total => _counts.Values.Sum();

        public IReadOnlyDictionary<PotionKey, int> Counts => _counts;

        public int Count(PotionKey key) => _counts.GetValueOrDefault(key, 0);

        public bool TryTake(PotionKey key)
        {
            var count = Count(key);
            switch (count)
            {
                case <= 0:
                    return false;
                case 1:
                    _counts.Remove(key);
                    break;
                default:
                    _counts[key] = count - 1;
                    break;
            }

            return true;
        }

        public void Add(PotionKey key, int count)
        {
            if (key.IsNone || count <= 0)
                return;
            _counts[key] = Count(key) + count;
        }

        public void Clear() => _counts.Clear();

        public List<SavedPotionStock> ToSaved()
        {
            return _counts.Select(pair => new SavedPotionStock(pair.Key.Tier, pair.Key.Category, pair.Value)).ToList();
        }

        // NOTE: Entries with a tier that's no longer defined, or a bad count, are dropped.
        public void Restore(IReadOnlyList<SavedPotionStock> saved, int tierCount)
        {
            _counts.Clear();
            foreach (var entry in saved)
            {
                if (entry.Tier < 1 || entry.Tier > tierCount || entry.Count <= 0)
                    continue;
                Add(new PotionKey(entry.Tier, entry.Category), entry.Count);
            }
        }
    }
}