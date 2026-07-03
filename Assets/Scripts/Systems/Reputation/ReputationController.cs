// Singleton owning the clamped reputation value; applies deltas and exposes the active tier + effects.

using UnityEngine;

[DefaultExecutionOrder(-84)]
public class ReputationController : MonoSingleton<ReputationController>
{
    public int Value { get; private set; }
    public ReputationTier CurrentTier { get; private set; }

    private ReputationTierTable _table;
    
    public ReputationEffects Effects =>
        _table ? ReputationTierResolver.EffectsFor(Value, _table)
            : new ReputationEffects(1f, 1f, 0);
    
    protected override void OnSingletonAwake()
    {
        var cfg = GameConfig.Instance.Reputation;
        _table = cfg.tierTable;
        Value = Clamp(cfg.startingReputation);
        CurrentTier = ResolveTier();
    }

    private void OnEnable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onReputationChanged.AddListener(HandleReputationChanged);
    }

    private void OnDisable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onReputationChanged.RemoveListener(HandleReputationChanged);
    }
    
    private void HandleReputationChanged(int delta, ReputationChangeReason reason)
    {
        if (delta == 0)
            return;
        var previous = Value;
        Value = Clamp(Value + delta);
        if (Value == previous)
            return;

        var newTier = ResolveTier();
        if (newTier != CurrentTier)
        {
            CurrentTier = newTier;
            GameEventsRelay.Instance.RaiseReputationTierChanged(newTier);
        }
    }

    private ReputationTier ResolveTier()
        => _table ? ReputationTierResolver.Resolve(Value, _table).tier : ReputationTier.Unknown;

    private int Clamp(int v)
    {
        var config = GameConfig.Instance.Reputation;
        return v < config.minReputation ? config.minReputation
            : v > config.maxReputation ? config.maxReputation : v;
    }
}