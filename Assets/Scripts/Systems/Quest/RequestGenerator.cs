// Paces request production from weighted templates (reputation-scaled) and feeds the RequestBoard.

using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-70)]
public class RequestGenerator : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private RequestTemplate[] templates;
    [Tooltip("Fixed RNG seed for deterministic runs (0 = time-based random).")]
    [SerializeField] private int seed;
    
    private IRequestFactory _factory;
    private System.Random _rng;
    private float _dayCredit;
    
    private void Awake()
    {
        _rng = seed != 0 ? new System.Random(seed) : new System.Random();
        var config = GameConfig.Instance;
        _factory = new StandardRequestFactory(_rng, config.Quest.baseRequestExpirationDays, config.Time.daysPerSeason);
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
            Emit(today);
            interval = CurrentInterval();
        }
        
        if (_dayCredit > interval) _dayCredit = interval;
    }
    
    private void Emit(GameDate today)
    {
        var template = PickTemplate();
        if (!template)
            return;
        RequestBoard.Instance.Add(_factory.Create(IdService.Instance.Next(IdService.Request), template, today));
    }
    
    private bool HasCapacity() =>
        RequestBoard.Exists && RequestBoard.Instance.ActiveCount < GameConfig.Instance.Quest.maxActiveRequests;
    
    private float CurrentInterval()
    {
        var config = GameConfig.Instance.Quest;
        var mult = ReputationController.Exists ? ReputationController.Instance.Effects.requestRateMultiplier : 1f;
        var scaled = config.baseRequestIntervalDays / Mathf.Max(0.01f, mult);
        return Mathf.Max(config.minRequestIntervalDays, scaled);
    }
    
    private RequestTemplate PickTemplate()
    {
        if (templates == null || templates.Length == 0)
            return null;
        var rep = CurrentReputation();

        var total = templates.Where(t => t && rep >= t.MinReputation).Sum(t => t.SelectionWeight);
        if (total <= 0f)
            return null;

        var roll = (float)_rng.NextDouble() * total;
        RequestTemplate last = null;
        foreach (var t in templates)
        {
            if (!t || rep < t.MinReputation)
                continue;
            last = t;
            roll -= t.SelectionWeight;
            if (roll <= 0f)
                return t;
        }
        return last;
    }
    
    private int CurrentReputation()
        => ReputationController.Exists ? ReputationController.Instance.Value : GameConfig.Instance.Reputation.startingReputation;
}