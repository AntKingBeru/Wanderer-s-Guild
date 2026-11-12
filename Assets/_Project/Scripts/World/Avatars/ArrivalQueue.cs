// Adventurers waiting to appear at the entrance, let in one at a time a short gap apart (a returning party walks in as a line).
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class ArrivalQueue
    {
        private readonly Queue<int> _waiting = new();
        private readonly HashSet<int> _queued = new();
        private float _cooldown;

        // NOTE: Repeats are ignored (hourly presence checks call this again while they wait).
        public void Enqueue(int adventurerId)
        {
            if (_queued.Add(adventurerId))
                _waiting.Enqueue(adventurerId);
        }

        // NOTE: Lazy — the id is skipped when its turn comes.
        public void Remove(int adventurerId) => _queued.Remove(adventurerId);

        public void Clear()
        {
            _waiting.Clear();
            _queued.Clear();
            _cooldown = 0f;
        }

        // The next adventurer whose turn it is. The first after a quiet spell comes at once; each next one waits the gap.
        // NOTE: Pass the frame's delta to the first call only (0 to later calls in the same frame).
        public bool TryNext(float deltaTime, float gap, out int adventurerId)
        {
            adventurerId = -1;
            _cooldown = Mathf.Max(0f, _cooldown - deltaTime);

            while (_cooldown <= 0f && _waiting.Count > 0)
            {
                var id = _waiting.Dequeue();
                if (!_queued.Remove(id))
                    continue; // Removed while waiting.

                adventurerId = id;
                _cooldown = gap;
                return true;
            }
            return false;
        }
    }
}