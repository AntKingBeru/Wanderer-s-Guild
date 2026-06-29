// ScriptableObject prototype for an adventurer class: base stats, per-level growth, starting values.

using UnityEngine;

[CreateAssetMenu(fileName = "AdventurerClassTemplate", menuName = "Wanderer's Guild/Adventurer Class Template")]
public class AdventurerClassTemplate : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private AdventurerClass @class;
    [SerializeField] private ClassTier tier = ClassTier.Base;

    [Header("Base Stats (level 1)")]
    [SerializeField] private int strength = 5;
    [SerializeField] private int dexterity = 5;
    [SerializeField] private int endurance = 5;
    [SerializeField] private int wits = 5;
    [SerializeField] private int spirit = 5;

    [Header("Per-Level Growth")]
    [SerializeField] private int strengthGrowth = 1;
    [SerializeField] private int dexterityGrowth = 1;
    [SerializeField] private int enduranceGrowth = 1;
    [SerializeField] private int witsGrowth = 1;
    [SerializeField] private int spiritGrowth = 1;

    [Header("Stat Variance")]
    [Tooltip("Max random +/- applied to each base stat at creation.")]
    [SerializeField] private int statVariance = 2;

    [Header("Starting Gold (inclusive range)")]
    [SerializeField] private int startingGoldMin = 20;
    [SerializeField] private int startingGoldMax = 60;

    public AdventurerClass Class => @class;
    public ClassTier Tier => tier;
    
    public StatBlock GrowthPerLevel => new(strengthGrowth, dexterityGrowth, enduranceGrowth, witsGrowth, spiritGrowth);
    
    public StatBlock RollBaseStats(System.Random rng)
        => new(Vary(strength, rng), Vary(dexterity, rng), Vary(endurance, rng),
            Vary(wits, rng), Vary(spirit, rng));
    
    public int RollStartingGold(System.Random rng)
    {
        var low = Mathf.Min(startingGoldMin, startingGoldMax);
        var hi = Mathf.Max(startingGoldMin, startingGoldMax);
        return rng.Next(low, hi + 1);
    }

    private int Vary(int baseValue, System.Random rng)
    {
        var delta = statVariance <= 0 ? 0 : rng.Next(-statVariance, statVariance + 1);
        return Mathf.Max(1, baseValue + delta);
    }
}