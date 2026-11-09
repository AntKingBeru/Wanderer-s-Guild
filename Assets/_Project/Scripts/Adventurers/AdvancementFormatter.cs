// Display strings for class advancement: chooser option labels, the roster hint, and the trial confirmation.
using System.Text;

namespace WanderersGuild
{
    public static class AdvancementFormatter
    {
        // "Knight · cap B · Combat, Escort" (affinities, or "—").
        public static string Option(ClassData option)
        {
            StringBuilder text = new($"{option.DisplayName} · cap {option.MaxRank} · ");
            if (option.CategoryAffinities.Count == 0)
                return text.Append('—').ToString();

            for (var i = 0; i < option.CategoryAffinities.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");
                text.Append(option.CategoryAffinities[i]);
            }
            return text.ToString();
        }

        // "Ready to advance: 200g trial", or why not.
        public static string Hint(AdvancementCheck check) =>
            check.CanAdvance ? $"Ready to advance: {check.Fee:N0}g trial" : check.Reason;

        // "Begin the Knight trial?\n(−200g, 2 days)".
        public static string Confirm(ClassData option, long fee, int days) =>
            $"Begin the {option.DisplayName} trial?\n(-{fee:N0}g, {days} days)";
    }
}