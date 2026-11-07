// The brewing screen's order (pure): how many of each potion to brew; step up/down within the free slots; clamp; list it.
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class BrewOrder
    {
        private readonly Dictionary<PotionKey, int> _counts = new();
        private readonly List<PotionKey> _scratch = new();

        public int Total => _counts.Values.Sum();

        public int Count(PotionKey key) => _counts.GetValueOrDefault(key, 0);

        // +1 / −1 for a potion; never below 0, never past <paramref name="limit"/> in total. True if it changed.
        public bool Step(PotionKey key, int delta, int limit)
        {
            var count = Count(key);
            switch (delta)
            {
                case > 0 when Total >= limit:
                case < 0 when count <= 0:
                    return false;
            }

            count += delta > 0 ? 1 : -1;
            if (count <= 0)
                _counts.Remove(key);
            else
                _counts[key] = count;
            return true;
        }

        // Drops tiers above <paramref name="maxTier"/>, then trims the total to <paramref name="limit"/> (newest
        // tiers / last categories first). True if anything changed.
        public bool Clamp(int maxTier, int limit)
        {
            var changed = false;
            _scratch.Clear();
            _scratch.AddRange(_counts.Keys);
            foreach (var key in _scratch.Where(key => key.Tier > maxTier))
            {
                _counts.Remove(key);
                changed = true;
            }

            _scratch.Clear();
            _scratch.AddRange(_counts.Keys);
            _scratch.Sort((a, b) => a.Tier != b.Tier ? b.Tier.CompareTo(a.Tier) : b.Category.CompareTo(a.Category));
            foreach (var key in _scratch)
            {
                while (Total > limit && Count(key) > 0)
                {
                    Step(key, -1, limit);
                    changed = true;
                }
            }
            return changed;
        }

        public void Clear() => _counts.Clear();

        public List<(PotionKey key, int count)> ToList()
        {
            return _counts.Select(pair => (pair.Key, pair.Value)).ToList();
        }
    }
}