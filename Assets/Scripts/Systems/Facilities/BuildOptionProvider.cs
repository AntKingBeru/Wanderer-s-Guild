// Builds the list of available room options for a door: rank-gated types, each fit/afford-checked.

using System.Collections.Generic;

public class BuildOptionProvider
{
    private readonly FacilityData[] _buildable;

    public BuildOptionProvider(FacilityData[] buildable) => _buildable = buildable;
    
    public List<BuildOption> OptionsFor(DoorKey door)
    {
        var options = new List<BuildOption>();
        if (_buildable == null) return options;

        var rank = GuildController.Exists ? GuildController.Instance.CurrentRank : GuildRank.F;

        foreach (var data in _buildable)
        {
            if (!data || data.Footprint == null) continue;

            var facility = FacilityController.Exists ? FacilityController.Instance.Get(data.Type) : null;
            if (facility is not { HasNextLevel: true })
                continue;
            if (!data.TryGetLevel(facility.NextLevel, out var def)) continue;
            if ((int)rank < (int)def.requiredGuildRank)
                continue;

            var fits = BuildPlacementResolver.TryResolve(door, data.Footprint, out var origin);
            var affordable = !TreasuryController.Exists || TreasuryController.Instance.CanAfford(def.goldCost);

            options.Add(new BuildOption
            {
                Type = data.Type, Data = data, Origin = origin,
                Cost = def.goldCost, ConstructionHours = def.constructionHours,
                Fits = fits, Affordable = affordable,
                DisabledReason = !fits ? "Doesn't fit here" : (!affordable ? "Not enough gold" : null)
            });
        }
        return options;
    }
}