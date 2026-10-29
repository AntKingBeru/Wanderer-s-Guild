// The open requests (capacity-limited, one per template); pure state — RequestManager raises the events.
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class RequestBoard
    {
        private readonly List<ActiveRequest> _requests = new();
        private readonly int _capacity;

        public RequestBoard(int capacity) => _capacity = System.Math.Max(1, capacity);

        public IReadOnlyList<ActiveRequest> Requests => _requests;
        public int FreeSlots => System.Math.Max(0, _capacity - _requests.Count);

        public bool ContainsTemplate(string templateId)
        {
            return _requests.Any(request => request.Data.Id == templateId);
        }

        // False when the board is full or this template is already posted.
        public bool TryAdd(ActiveRequest request)
        {
            if (!request?.Data || FreeSlots == 0 || ContainsTemplate(request.Data.Id))
                return false;

            _requests.Add(request);
            return true;
        }

        public bool TryRemove(int instanceId, out ActiveRequest removed)
        {
            for (var i = 0; i < _requests.Count; i++)
            {
                if (_requests[i].InstanceId != instanceId)
                    continue;

                removed = _requests[i];
                _requests.RemoveAt(i);
                return true;
            }

            removed = null;
            return false;
        }

        // NOTE: Collected first, then removed — never removes while iterating.
        public List<ActiveRequest> TakeExpired(long nowMinutes)
        {
            var expired = _requests.Where(request => request.IsExpiredAt(nowMinutes)).ToList();

            foreach (var request in expired)
                _requests.Remove(request);

            return expired;
        }
    }
}