// ScriptableObject table mapping reputation bands to thresholds and their gameplay effects.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ReputationTierTable", menuName = "Wanderer's Guild/Reputation Tier Table")]
public class ReputationTierTable : ScriptableObject
{
    [Tooltip("Bands in ascending MinValue order. Sorted defensively at load.")]
    [SerializeField] private List<Band> bands = new List<Band>();
    
    public IReadOnlyList<Band> Bands => bands;
    
    private void OnEnable()
        => bands.Sort((a, b) => a.minValue.CompareTo(b.minValue));
    
    private void Reset()
    {
        bands = new List<Band>
        {
            new() { tier = ReputationTier.Reviled, minValue = -100, arrivalRateMultiplier = 0.4f, requestRateMultiplier = 0.5f, arrivalQualityBonus = 0 },
            new() { tier = ReputationTier.Distrusted, minValue = -60, arrivalRateMultiplier = 0.7f, requestRateMultiplier = 0.75f, arrivalQualityBonus = 0 },
            new() { tier = ReputationTier.Unknown, minValue = -20, arrivalRateMultiplier = 1.0f, requestRateMultiplier = 1.0f, arrivalQualityBonus = 0 },
            new() { tier = ReputationTier.Recognized, minValue = 20, arrivalRateMultiplier = 1.4f, requestRateMultiplier = 1.25f, arrivalQualityBonus = 1 },
            new() { tier = ReputationTier.Respected, minValue = 55, arrivalRateMultiplier = 1.8f, requestRateMultiplier = 1.5f, arrivalQualityBonus = 2 },
            new() { tier = ReputationTier.Renowned, minValue = 85, arrivalRateMultiplier = 2.4f, requestRateMultiplier = 2.0f, arrivalQualityBonus = 3 },
        };
    }
}