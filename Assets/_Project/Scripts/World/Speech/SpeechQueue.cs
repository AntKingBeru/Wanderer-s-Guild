// Lines waiting to be said, per adventurer: most important first, capped, and dropped when stale or when their speaker is
// gone (lines that wait for a return excepted).
using System;
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Event Queue — producers (SpeechTriggers, debug) and the consumer (SpeechBubbleDirector) never talk directly,
    // so the order of event listeners doesn't matter.
    public sealed class SpeechQueue
    {
        private readonly Dictionary<int, List<SpeechRequest>> _waiting = new();
        private readonly List<int> _emptied = new();

        public bool HasAny(int adventurerId) => _waiting.TryGetValue(adventurerId, out var list) && list.Count > 0;

        // NOTE: Chatter only joins an empty queue; a topic already waiting isn't queued twice; inserted after every request
        // of equal or higher priority; past the cap the last (least important) one is dropped.
        public void Enqueue(SpeechRequest request, int cap)
        {
            cap = Math.Max(1, cap);
            if (!_waiting.TryGetValue(request.AdventurerId, out var list))
                _waiting[request.AdventurerId] = list = new List<SpeechRequest>(cap + 1);

            if (SpeechRules.IsChatter(request.Topic) && list.Count > 0)
                return;
            foreach (var waiting in list)
            {
                if (waiting.Topic == request.Topic)
                    return;
            }

            var index = list.Count;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Priority >= request.Priority)
                    continue;
                index = i;
                break;
            }
            list.Insert(index, request);

            while (list.Count > cap)
                list.RemoveAt(list.Count - 1);
        }

        // Removes and returns the most important request that's due, if any.
        public bool TryTakeDue(int adventurerId, long now, out SpeechRequest request)
        {
            request = default;
            if (!_waiting.TryGetValue(adventurerId, out var list))
                return false;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].ShowAtMinute > now)
                    continue;
                request = list[i];
                list.RemoveAt(i);
                return true;
            }
            return false;
        }

        // NOTE: Every frame: expired lines go; so do lines of absent speakers unless they wait for the speaker's arrival.
        public void Prune(long now, Func<int, bool> isPresent)
        {
            _emptied.Clear();
            foreach (var (id, list) in _waiting)
            {
                var present = isPresent(id);
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var request = list[i];
                    if (now > request.ExpiresAtMinute || (!present && !request.WaitsForArrival))
                        list.RemoveAt(i);
                }

                if (list.Count == 0)
                    _emptied.Add(id);
            }

            foreach (var id in _emptied)
                _waiting.Remove(id);
        }

        public void Clear(int adventurerId) => _waiting.Remove(adventurerId);
    }
}