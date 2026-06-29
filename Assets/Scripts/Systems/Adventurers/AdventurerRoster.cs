// Singleton registry of all guild adventurers; stores by id and announces additions/departures.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class AdventurerRoster : MonoSingleton<AdventurerRoster>
{
    private readonly Dictionary<int, Adventurer> _adventurers = new Dictionary<int, Adventurer>();

    public int Count => _adventurers.Count;
    
    public bool Add(Adventurer adventurer)
    {
        if (adventurer == null || !_adventurers.TryAdd(adventurer.Id, adventurer))
            return false;
        GameEventsRelay.Instance.RaiseAdventurerRecruited(adventurer.Id);
        return true;
    }

    public Adventurer Get(int id)
        => _adventurers.GetValueOrDefault(id);
    
    public IReadOnlyList<Adventurer> GetAll()
        => new List<Adventurer>(_adventurers.Values);

    public bool Remove(int id, DepartureReason reason)
    {
        if (!_adventurers.Remove(id))
            return false;
        GameEventsRelay.Instance.RaiseAdventurerDeparted(id, reason);
        return true;
    }
}