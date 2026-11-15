// Which flag a roster row shows: ready for a class advancement trial, ready for a promotion exam, or none.
namespace WanderersGuild
{
    // NOTE: Exclusive by rule — promotion needs a rank under the class cap, advancement needs the cap. Gold is left out (the
    // fee shows in the details hint), so the flags don't flicker as gold changes.
    public static class RosterReadinessRules
    {
        public static RosterReadiness Of(Adventurer adventurer)
        {
            if (adventurer == null)
                return RosterReadiness.None;
            if (AdvancementManager.HasInstance && AdvancementManager.Instance.IsReady(adventurer))
                return RosterReadiness.Advancement;
            if (PromotionManager.HasInstance && PromotionManager.Instance.IsReady(adventurer))
                return RosterReadiness.Promotion;
            return RosterReadiness.None;
        }
    }
}