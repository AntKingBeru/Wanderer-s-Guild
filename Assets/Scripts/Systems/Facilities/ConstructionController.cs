// Singleton Observer: advances active facility construction jobs each in-game hour and completes them.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-64)]
public class ConstructionController : MonoSingleton<ConstructionController>
{
    private class Job { public FacilityType Type; public int TargetLevel; public int HoursRemaining; public int TotalHours; }

    private readonly Dictionary<FacilityType, Job> _jobs = new Dictionary<FacilityType, Job>();

    public bool IsUnderConstruction(FacilityType type)
        => _jobs.ContainsKey(type);

    private void OnEnable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onHourAdvanced.AddListener(HandleHourAdvanced);
    }
    
    private void OnDisable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onHourAdvanced.RemoveListener(HandleHourAdvanced);
    }
    
    public bool Enqueue(FacilityType type, int targetLevel, int hours)
    {
        if (_jobs.ContainsKey(type))
            return false;
        _jobs[type] = new Job { Type = type, TargetLevel = targetLevel,
            HoursRemaining = Mathf.Max(1, hours), TotalHours = Mathf.Max(1, hours) };
        return true;
    }
    
    private void HandleHourAdvanced(int hour)
    {
        if (_jobs.Count == 0)
            return;
        
        var done = new List<FacilityType>();
        foreach (var kvp in _jobs)
        {
            kvp.Value.HoursRemaining--;
            if (kvp.Value.HoursRemaining <= 0)
                done.Add(kvp.Key);
        }

        foreach (var type in done)
        {
            _jobs.Remove(type);
            if (FacilityController.Exists)
                FacilityController.Instance.CompleteConstruction(type);
        }
    }
    
    public float GetProgress(FacilityType type)
        => !_jobs.TryGetValue(type, out var job) ? 1f : Mathf.Clamp01(1f - (float)job.HoursRemaining / job.TotalHours);
}