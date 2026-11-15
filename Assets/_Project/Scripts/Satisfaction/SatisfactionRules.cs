// Satisfaction rules (pure): score → level, what each level asks for to sign up and adds to quest power, pay and injury scale.
using UnityEngine;

namespace WanderersGuild
{
    public static class SatisfactionRules
    {
        public static float Score(float baseline, float memories, float situations) =>
            Mathf.Clamp(baseline + memories + situations, 0f, 100f);

        public static SatisfactionLevel LevelFor(float score, SatisfactionSettings settings)
        {
            if (score < settings.MiserableBelow)
                return SatisfactionLevel.Miserable;
            if (score < settings.UnhappyBelow)
                return SatisfactionLevel.Unhappy;
            return score >= settings.HappyFrom ? SatisfactionLevel.Happy : SatisfactionLevel.Content;
        }

        // The appeal a quest must reach for them to sign up (1 = their normal expectation).
        public static float RequiredAppeal(SatisfactionLevel level, SatisfactionSettings settings) => level switch
        {
            SatisfactionLevel.Miserable => settings.MiserableAppeal,
            SatisfactionLevel.Unhappy => settings.UnhappyAppeal,
            _ => 1f
        };

        public static float PowerBonus(SatisfactionLevel level, SatisfactionSettings settings) =>
            level == SatisfactionLevel.Happy ? settings.HappyPowerBonus : 0f;

        // NOTE: Good / Poor Pay and how many tens of percent from the fair share; false at exactly the fair share.
        public static bool TryPay(int sharePercent, SatisfactionSettings settings, out MoodReason reason, out float scale)
        {
            var difference = sharePercent - settings.FairSharePercent;
            reason = difference > 0 ? MoodReason.GoodPay : MoodReason.PoorPay;
            scale = Mathf.Abs(difference) / 10f;
            return difference != 0;
        }

        // Light ×0.5, Serious ×1, Grave ×1.5.
        public static float InjuryScale(InjurySeverity severity) => (int)severity * 0.5f;
    }
}