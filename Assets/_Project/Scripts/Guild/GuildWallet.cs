// The guild's gold: never below 0; spending is refused when the guild can't afford it. Raises OnGoldChanged.
namespace WanderersGuild
{
    public sealed class GuildWallet
    {
        public long Gold { get; private set; }

        public GuildWallet(long startingGold) => Gold = System.Math.Max(0L, startingGold);

        public bool CanAfford(long amount) => amount >= 0 && Gold >= amount;

        /// <param name="amount"></param>
        /// <param name="reason">Why the gold came in (kept for logging and later reactions).</param>
        public void Add(long amount, GoldReason reason)
        {
            if (amount <= 0) return;

            // NOTE: Saturates instead of overflowing.
            var added = amount > long.MaxValue - Gold ? long.MaxValue - Gold : amount;
            if (added == 0)
                return;

            Gold += added;
            GameEventsRelay.OnGoldChanged.Invoke(Gold, added);
        }

        // False (and nothing changes) when the guild can't afford it. Zero costs succeed; negative ones are refused.
        public bool TrySpend(long amount, GoldReason reason)
        {
            if (amount == 0)
                return true;
            if (amount < 0 || Gold < amount)
                return false;

            Gold -= amount;
            GameEventsRelay.OnGoldChanged.Invoke(Gold, -amount);
            return true;
        }

        // Raises the current total with no change (scene start).
        public void PublishInitial() => GameEventsRelay.OnGoldChanged.Invoke(Gold, 0L);
    }
}