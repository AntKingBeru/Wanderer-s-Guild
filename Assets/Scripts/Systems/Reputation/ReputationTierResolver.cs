// Pure resolver: maps a reputation value to its active tier and derived effects via the table.

public static class ReputationTierResolver
{
    public static Band Resolve(int value, ReputationTierTable table)
    {
        var bands = table.Bands;
        for (var i = bands.Count - 1; i >= 0; i--)
            if (value >= bands[i].minValue)
                return bands[i];
        
        return bands.Count > 0 ? bands[0] : default;
    }

    public static ReputationEffects EffectsFor(int value, ReputationTierTable table)
    {
        var band = Resolve(value, table);
        return new ReputationEffects(band.arrivalRateMultiplier, band.requestRateMultiplier, band.arrivalQualityBonus);
    }
}