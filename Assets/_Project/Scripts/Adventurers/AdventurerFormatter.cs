// Display strings for adventurers: species · class, rank · level, XP, rank progress, stats, wealth, affinities, status, promotion hint.
using System.Text;
using System.Globalization;

namespace WanderersGuild
{
    public static class AdventurerFormatter
    {
        public static string SpeciesAndClass(Adventurer adventurer) =>
            $"{adventurer.Species.DisplayName} · {adventurer.Class.DisplayName}";

        public static string RankAndLevel(Adventurer adventurer) => $"Rank {adventurer.Rank} · Level {adventurer.Level}";

        public static string ShortLevel(Adventurer adventurer) => $"Lv {adventurer.Level}";

        // "XP 1,200 / 2,700" — total XP toward the next level's threshold; "MAX" at the cap.
        public static string Experience(Adventurer adventurer, ExperienceTable table)
        {
            if (adventurer.IsMaxLevel)
                return "XP MAX";

            var next = table.ThresholdFor(adventurer.Level + 1);
            return $"XP {Number(adventurer.Experience)} / {Number(next)}";
        }

        // "Rank 2.5/5", or "Rank MAX" at the highest rank.
        public static string RankProgress(Adventurer adventurer, PromotionSettings settings) =>
            adventurer.Rank >= GuildRank.National
                ? "Rank MAX"
                : $"Rank {adventurer.RankProgress.ToString("0.#", CultureInfo.InvariantCulture)}/" +
                  $"{settings.ProgressRequired.ToString("0.#", CultureInfo.InvariantCulture)}";

        // Bar fill 0–1 (full at the highest rank).
        public static float RankProgressFill(Adventurer adventurer, PromotionSettings settings) =>
            adventurer.Rank >= GuildRank.National ? 1f : adventurer.RankProgress / settings.ProgressRequired;

        public static string StatsTop(StatBlock stats) => $"STR {stats.Strength}  AGI {stats.Agility}";
        public static string StatsBottom(StatBlock stats) => $"INT {stats.Intellect}  VIT {stats.Vitality}";

        public static string Wealth(Adventurer adventurer) => $"Wealth: {Number(adventurer.Wealth)}g";

        public static string Affinities(Adventurer adventurer)
        {
            var affinities = adventurer.Class.CategoryAffinities;
            if (affinities.Count == 0)
                return "Good at: —";

            StringBuilder text = new("Good at: ");
            for (var i = 0; i < affinities.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");
                text.Append(affinities[i]);
            }
            return text.ToString();
        }

        // "Status: Idle" / "Signed up" / "On quest" / "In exam (back 1d)".
        public static string Status(Adventurer adventurer, long nowMinutes)
        {
            switch (adventurer.Status)
            {
                case AdventurerStatus.SignedUp: return "Status: Signed up";
                case AdventurerStatus.OnQuest: return "Status: On quest";
                case AdventurerStatus.InExam:
                    var left = System.Math.Max(0L, adventurer.ExamReturnsAtMinutes - nowMinutes);
                    return $"Status: In exam (back {RequestFormatter.TimeLeft(left)})";
                default: return "Status: Idle";
            }
        }

        // "Rank E exam ready: 50g", or why not.
        public static string PromotionHint(PromotionCheck check) =>
            check.CanPromote ? $"Rank {check.Target} exam ready: {check.Fee:N0}g" : check.Reason;

        private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}