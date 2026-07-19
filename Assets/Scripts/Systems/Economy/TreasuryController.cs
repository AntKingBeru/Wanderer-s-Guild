// Singleton treasury: the sole authority on guild gold; applies income/spends and broadcasts changes.

using UnityEngine;

[DefaultExecutionOrder(-84)]
public class TreasuryController : MonoSingleton<TreasuryController>
{
    private TreasuryLedger _ledger;

    public int Gold => _ledger.Balance;

    protected override void OnSingletonAwake() =>
        _ledger = new TreasuryLedger(GameConfig.Instance.Economy.startingGold);

    private void OnEnable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onQuestOutcome.AddListener(HandleQuestOutcome);
    }

    private void OnDisable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onQuestOutcome.RemoveListener(HandleQuestOutcome);
    }

    public bool CanAfford(int amount)
        => _ledger.CanAfford(amount);
    
    public bool TrySpend(int amount, TransactionType type)
    {
        if (amount <= 0)
            return true;
        var tx = new Transaction(type, -amount);

        if (!_ledger.TryApply(-amount))
        {
            GameEventsRelay.Instance.RaiseTransactionRejected(tx);
            return false;
        }
        Announce(tx);
        return true;
    }
    
    public void Earn(int amount, TransactionType type)
    {
        if (amount <= 0)
            return;
        var tx = new Transaction(type, amount);
        _ledger.TryApply(amount);
        Announce(tx);
    }
    
    private void HandleQuestOutcome(int questId, QuestOutcome outcome)
    {
        if (outcome is { success: true, goldToGuild: > 0 })
            Earn(outcome.goldToGuild, TransactionType.QuestReward);
    }
    
    private void Announce(Transaction tx)
    {
        var relay = GameEventsRelay.Instance;
        relay.RaiseTransaction(tx);
        relay.RaiseGoldChanged(_ledger.Balance, tx.amount);
    }
}