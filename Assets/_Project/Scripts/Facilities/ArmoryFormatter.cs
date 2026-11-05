// Display strings for the armory screen: title, row info, units, item details (slot, users, stats) and the restocking footer.
using System.Collections.Generic;
using System.Text;

namespace WanderersGuild
{
    public static class ArmoryFormatter
    {
        public static string Title(int tier) => tier > 0 ? $"Armory · Tier {tier}" : "Armory";

        // "Weapon · 7g".
        public static string RowInfo(EquipmentData item) => $"{item.Slot} · {item.Price:N0}g";

        public static string Units(int units, int max) => $"{units}/{max}";

        // "Weapon · Two-handed" / "Armor".
        public static string Slot(EquipmentData item) => item.TwoHanded ? $"{item.Slot} · Two-handed" : item.Slot.ToString();

        // "For: Fighter, Archer" or "For: anyone".
        public static string Users(EquipmentData item, IReadOnlyDictionary<string, ClassData> classes)
        {
            if (item.ClassIds.Count == 0)
                return "For: anyone";

            StringBuilder text = new("For: ");
            for (var i = 0; i < item.ClassIds.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");
                text.Append(classes.TryGetValue(item.ClassIds[i], out var c) ? c.DisplayName : item.ClassIds[i]);
            }
            return text.ToString();
        }

        // "STR +2  VIT +1" (non-zero bonuses only), or "No stat bonus".
        public static string Stats(StatBlock stats)
        {
            StringBuilder text = new();
            Append(text, "STR", stats.Strength);
            Append(text, "AGI", stats.Agility);
            Append(text, "INT", stats.Intellect);
            Append(text, "VIT", stats.Vitality);
            return text.Length > 0 ? text.ToString() : "No stat bonus";
        }

        public static string Price(EquipmentData item) => $"Price: {item.Price:N0}g";

        public static string InStock(int units, int max) => $"In stock: {units} / {max}";

        // "Next restock 06:00 (in 5h) · 14g", or "Fully stocked".
        public static string Footer(long fullCost, long minutesUntilDawn, int dawnHour) =>
            fullCost <= 0
                ? "Fully stocked"
                : $"Next restock {dawnHour:00}:00 (in {RequestFormatter.TimeLeft(minutesUntilDawn)}) · {fullCost:N0}g";

        private static void Append(StringBuilder text, string label, int value)
        {
            if (value == 0)
                return;
            if (text.Length > 0)
                text.Append("  ");
            text.Append(label).Append(value > 0 ? " +" : " ").Append(value);
        }
    }
}