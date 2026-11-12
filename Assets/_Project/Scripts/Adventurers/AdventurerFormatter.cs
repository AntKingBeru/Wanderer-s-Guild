// Display strings for adventurers: given name, species · class, rank · level, XP, rank progress, stats, wealth, affinities, gear, status, promotion hint.
using System.Text;
using System.Globalization;

namespace WanderersGuild
{
    public static class AdventurerFormatter
    {
        // The given name — a full name's first word ("Dagny Ironfoot" → "Dagny"). NOTE: Names are generated as one-word given
        // names plus an optional family name or epithet, so the first word is always the given name.
        public static string GivenName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;

            var space = fullName.IndexOf(' ');
            return space < 0 ? fullName : fullName[..space];
        }
        
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

        // "Gear: Sword, Shield, Leather Armor" (slot order), or "Gear: —".
        public static string Gear(Adventurer adventurer)
        {
            StringBuilder text = new("Gear: ");
            var any = false;
            foreach (var item in adventurer.Equipment.Worn)
            {
                if (any)
                    text.Append(", ");
                text.Append(item.DisplayName);
                any = true;
            }
            return any ? text.ToString() : "Gear: —";
        }

        // "Status: Idle" / "Signed up" / "On quest" / "In exam (back 1d)" / "Injured (serious, 3d)", plus
        // " · Well fed (+6%)" when fed.
        public static string Status(Adventurer adventurer, long nowMinutes)
        {
            var status = adventurer.Status switch
            {
                AdventurerStatus.SignedUp => "Status: Signed up",
                AdventurerStatus.OnQuest => "Status: On quest",
                AdventurerStatus.InExam =>
                    $"Status: In exam (back {RequestFormatter.TimeLeft(System.Math.Max(0L, adventurer.ExamReturnsAtMinutes - nowMinutes))})",
                AdventurerStatus.Injured =>
                    $"Status: Injured ({adventurer.Injury.ToString().ToLowerInvariant()}, " +
                    $"{RequestFormatter.TimeLeft(System.Math.Max(0L, adventurer.InjuredUntilMinutes - nowMinutes))})",
                AdventurerStatus.Returning =>
                    $"Status: Returning (back {RequestFormatter.TimeLeft(System.Math.Max(0L, adventurer.ReturningUntilMinutes - nowMinutes))})",
                AdventurerStatus.InTrial =>
                    $"Status: In trial ({TrialClassName(adventurer)}, back " +
                    $"{RequestFormatter.TimeLeft(System.Math.Max(0L, adventurer.TrialReturnsAtMinutes - nowMinutes))})",
                _ => "Status: Idle"
            };

            return adventurer.IsWellFed
                ? $"{status} · Well fed (+{UnityEngine.Mathf.RoundToInt(adventurer.WellFedBonus * 100f)}%)"
                : status;
        }

        // "Rank E exam ready: 50g", or why not.
        public static string PromotionHint(PromotionCheck check) =>
            check.CanPromote ? $"Rank {check.Target} exam ready: {check.Fee:N0}g" : check.Reason;

        private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
        
        private static string TrialClassName(Adventurer adventurer) =>
            AdventurerManager.HasInstance && AdventurerManager.Instance.TryGetClass(adventurer.TrialClassId, out var c)
                ? c.DisplayName
                : adventurer.TrialClassId;
    }
}