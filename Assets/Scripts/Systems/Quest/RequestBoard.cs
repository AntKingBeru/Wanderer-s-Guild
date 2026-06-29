// Singleton registry of active requests; stores by id and expires them as days pass.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class RequestBoard : MonoSingleton<RequestBoard>
{
    private readonly Dictionary<int, Request> _requests = new Dictionary<int, Request>();
    private readonly List<int> _expiryScratch = new List<int>();
    
    public int ActiveCount => _requests.Count;
    
    private void OnEnable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onDayAdvanced.AddListener(HandleDayAdvanced);
    }

    private void OnDisable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onDayAdvanced.RemoveListener(HandleDayAdvanced);
    }
    
    public bool Add(Request request)
    {
        if (request == null || !_requests.TryAdd(request.Id, request))
            return false;
        GameEventsRelay.Instance.RaiseRequestGenerated(request.Id);
        return true;
    }
    
    public Request Get(int id)
        => _requests.GetValueOrDefault(id);
    
    public IReadOnlyList<Request> GetAll()
        => new List<Request>(_requests.Values);
    
    public bool Remove(int id)
        => _requests.Remove(id);
    
    private void HandleDayAdvanced(GameDate today)
    {
        _expiryScratch.Clear();
        foreach (var kvp in _requests.Where(kvp => kvp.Value.IsExpired(today)))
            _expiryScratch.Add(kvp.Key);

        foreach (var id in _expiryScratch)
        {
            _requests.Remove(id);
            GameEventsRelay.Instance.RaiseRequestExpired(id);
        }
    }
}