// Singleton registry of named world-space props (reception desks, guild boards, exit point).
// Each prop self-registers on Awake via RegisterPoint().
// Other systems call GetRandomPoint() to get a destination without needing direct scene references.
// Uses the Service Locator / Registry pattern so AdventurerNavigationController never needs scene-level inspector wiring to individual props.

using System.Collections.Generic;
using UnityEngine;

public class GuildPointRegistry : MonoBehaviour
{
    public static GuildPointRegistry Instance { get; private set; }
    
    // Internal lists keyed by point type.
    private readonly Dictionary<GuildPointType, List<Transform>> _points = new();
    
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Pre-populate all keys so callers never get a KeyNotFoundException.
        foreach (GuildPointType t in System.Enum.GetValues(typeof(GuildPointType)))
            _points[t] = new List<Transform>();
    }
    
    // Called by each prop's GuildPointMarker component on Awake.
    public void RegisterPoint(GuildPointType type, Transform point)
    {
        if (!point)
            return;
        _points[type].Add(point);
    }
    
    // Called when a prop is destroyed or disabled.
    public void UnregisterPoint(GuildPointType type, Transform point)
    {
        if (_points.TryGetValue(type, out var list))
            list.Remove(point);
    }
    
    // Returns a random transform of the given type, or null if none are registered.
    public Transform GetRandomPoint(GuildPointType type)
    {
        if (!_points.TryGetValue(type, out var list) || list.Count == 0)
        {
            Debug.LogWarning($"[GuildPointsRegistry] No points registered for {type}.");
            return null;
        }
        return list[Random.Range(0, list.Count)];
    }

    // Returns all registered transforms of a given type (read-only snapshot).
    public IReadOnlyList<Transform> GetAllPoints(GuildPointType type)
        => _points.TryGetValue(type, out var list) ? list : new List<Transform>();
}