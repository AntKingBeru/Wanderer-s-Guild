// Singleton dispensing unique monotonic ids per category so seeded/generated content never collide.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class IdService : MonoSingleton<IdService>
{
    private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();
    
    public const string Request = "request";
    public const string Quest = "quest";
    public const string Adventurer = "adventurer";
    public const string Party = "party";
    public const string Application = "application";
    
    public int Next(string category)
    {
        _counters.TryGetValue(category, out var last);
        var next = last + 1;
        _counters[category] = next;
        return next;
    }
    
    public void SetCounter(string category, int value)
    {
        if (!_counters.TryGetValue(category, out var cur) || value > cur)
            _counters[category] = value;
    }
}