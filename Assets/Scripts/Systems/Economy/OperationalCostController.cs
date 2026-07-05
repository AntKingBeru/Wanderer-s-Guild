// Observer that deducts the guild's daily operational cost from the treasury each day.

using UnityEngine;

[DefaultExecutionOrder(-60)]
public class OperationalCostController : MonoBehaviour
{
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
        var cost = GameConfig.Instance.Economy.dailyOperationCost;
        if (cost <= 0 || !TreasuryController.Exists)
            return;
        TreasuryController.Instance.TrySpend(cost, TransactionType.OperationalCost);
    }
}

