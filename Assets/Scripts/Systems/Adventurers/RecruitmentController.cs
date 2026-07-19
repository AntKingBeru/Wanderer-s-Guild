// Paces natural adventurer arrivals (reputation-scaled) from class templates into the roster.

using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-70)]
public class RecruitmentController : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private AdventurerClassTemplate[] baseClasses;
    [SerializeField] private NamePool namePool;
    [Tooltip("Fixed RNG seed for deterministic runs (0 = time-based random).")]
    [SerializeField] private int seed;

    private IAdventurerFactory _factory;
    private System.Random _rng;
    private float _dayCredit;
    
    private void Awake()
    {
        _rng = seed != 0 ? new System.Random(seed) : new System.Random();
        _factory = new StandardAdventurerFactory(_rng, namePool,
            GameConfig.Instance.Adventurer.baseExperiencePerLevel);
    }

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
    
    private void HandleDayAdvanced(GameDate today)
    {
        _dayCredit += 1f;
        var interval = CurrentInterval();
        while (_dayCredit >= interval && HasCapacity())
        {
            _dayCredit -= interval;
            Recruit();
            interval = CurrentInterval();
        }
        if (_dayCredit > interval) _dayCredit = interval;
    }

    private void Recruit()
    {
        var template = PickClass();
        if (!template)
            return;
        AdventurerRoster.Instance.Add(_factory.Create(IdService.Instance.Next(IdService.Adventurer), template, RolledArrivalRank()));
    }

    private bool HasCapacity()
    {
        if (!AdventurerRoster.Exists)
            return false;
        var cap = FacilityController.Exists
            ? FacilityController.Instance.AdventurerCapacity
            : GameConfig.Instance.Facilities.baseAdventurerCapacity;
        return AdventurerRoster.Instance.Count < cap;
    }
    
    private float CurrentInterval()
    {
        var config = GameConfig.Instance.Adventurer;
        var mult = ReputationController.Exists ? ReputationController.Instance.Effects.arrivalRateMultiplier : 1f;
        var scaled = config.baseArrivalIntervalDays / Mathf.Max(0.01f, mult);
        return Mathf.Max(config.minArrivalIntervalDays, scaled);
    }
    
    private AdventurerClassTemplate PickClass()
    {
        if (baseClasses == null)
            return null;
        var valid = baseClasses.Count(t => t);
        if (valid == 0)
            return null;

        var target = _rng.Next(valid);
        return baseClasses.Where(t => t).FirstOrDefault(_ => target-- == 0);
    }
    
    private GuildRank RolledArrivalRank()
    {
        var cap = GameConfig.Instance.Adventurer.defaultRankCap;
        var bonusRolls = ReputationController.Exists ? ReputationController.Instance.Effects.arrivalQualityBonus : 0;
        var rank = GuildRank.F;
        for (var i = 0; i < bonusRolls && rank < cap; i++)
        {
            if (_rng.NextDouble() < 0.5)
                rank = (GuildRank)((int)rank + 1);
            else break;
        }
        return rank;
    }
}