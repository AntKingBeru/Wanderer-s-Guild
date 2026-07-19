// Pure guild-gold ledger: holds the balance and validates/applies spends and income. No Unity deps.

public class TreasuryLedger
{
    public int Balance { get; private set; }

    public TreasuryLedger(int startingBalance)
        => Balance = System.Math.Max(0, startingBalance);

    public bool CanAfford(int amount) => amount <= Balance;
    
    public bool TryApply(int delta)
    {
        if (delta == 0)
            return true;
        if (delta < 0 && -delta > Balance)
            return false;
        Balance += delta;
        return true;
    }
}