// Minimal facility presence tracker (stub): records which facilities are built. Full system later.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-85)]
public class FacilityController : MonoSingleton<FacilityController>
{
    [Tooltip("One FacilityData asset per facility type available in the guild.")]
    [SerializeField] private FacilityData[] facilityData;
    
    private readonly Dictionary<FacilityType, Facility> _facilities = new Dictionary<FacilityType, Facility>();
    private int _lastCapacity;
    
    protected override void OnSingletonAwake()
    {
        if (facilityData != null)
            foreach (var data in facilityData)
                if (data && !_facilities.ContainsKey(data.Type))
                    _facilities[data.Type] = new Facility(data);

        _lastCapacity = ComputeCapacity();
    }

    public bool IsBuilt(FacilityType type) => _facilities.TryGetValue(type, out var f) && f.Level >= 1;

    public int GetLevel(FacilityType type) => _facilities.TryGetValue(type, out var f) ? f.Level : 0;
    public Facility Get(FacilityType type) => _facilities.GetValueOrDefault(type);
    
    public int AdventurerCapacity => ComputeCapacity();
    
    public bool StartConstruction(FacilityType type, out string error)
    {
        error = null;
        if (!_facilities.TryGetValue(type, out var f))
        {
            error = "Unknown facility.";
            return false;
        }

        if (!f.HasNextLevel)
        {
            error = "Facility is at max level.";
            return false;
        }

        if (ConstructionController.Exists && ConstructionController.Instance.IsUnderConstruction(type))
        {
            error = "Already under construction.";
            return false;
        }

        if (!f.Data.TryGetLevel(f.NextLevel, out var def))
        {
            error = "No data for next level.";
            return false;
        }

        if (GuildController.Exists && (int)GuildController.Instance.CurrentRank < (int)def.requiredGuildRank)
        {
            error = "Guild rank too low.";
            return false;
        }

        f.SetState(f.Level == 0 ? FacilityState.UnderConstruction : FacilityState.Upgrading);
        ConstructionController.Instance.Enqueue(type, f.NextLevel, def.constructionHours);
        GameEventsRelay.Instance.RaiseFacilityConstructionStarted(type);
        return true;
    }
    
    public void CompleteConstruction(FacilityType type)
    {
        if (!_facilities.TryGetValue(type, out var f))
            return;

        var firstBuild = f.Level == 0;
        f.CompleteConstruction();

        var relay = GameEventsRelay.Instance;
        if (firstBuild)
            relay.RaiseFacilityBuilt(type);
        else
            relay.RaiseFacilityUpgraded(type);

        RecomputeCapacity();
    }
    
    public void MarkBuilt(FacilityType type)
    {
        if (!_facilities.TryGetValue(type, out var f) || f.Level >= 1)
            return;
        f.CompleteConstruction();
        GameEventsRelay.Instance.RaiseFacilityBuilt(type);
        RecomputeCapacity();
    }

    private void RecomputeCapacity()
    {
        var cap = ComputeCapacity();
        if (cap == _lastCapacity)
            return;
        _lastCapacity = cap;
        GameEventsRelay.Instance.RaiseAdventurerCapacityChanged(cap);
    }

    private int ComputeCapacity()
    {
        var config = GameConfig.Instance.Facilities;
        return config.baseAdventurerCapacity + config.capacityPerBedroomLevel * GetLevel(FacilityType.Bedroom);
    }
}