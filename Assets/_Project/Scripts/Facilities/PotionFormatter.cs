// Display strings for potions: "Minor Combat Potion", "Minor (2)" labels, the effect line, the brew toast.
namespace WanderersGuild
{
    public static class PotionFormatter
    {
        // "Minor Combat Potion".
        public static string Name(PotionKey key, PotionTier tier) => $"{tier.Name} {key.Category} Potion";

        // "Potions: None" / "Potions: Minor (2)".
        public static string Label(PotionKey key, PotionTier tier, int inStock) =>
            key.IsNone ? "Potions: None" : $"Potions: {tier.Name} ({inStock})";

        // "−10% share · +8% chance" (empty for None).
        public static string Effect(PotionKey key, PotionTier tier) =>
            key.IsNone
                ? string.Empty
                : $"-{tier.ShareCutPercent}% share · +{UnityEngine.Mathf.RoundToInt(tier.ChanceBonus * 100f)}% chance";

        public static string Brewed(int count, long cost) =>
            count == 1 ? $"Brewed 1 potion (-{cost:N0}g)" : $"Brewed {count} potions (-{cost:N0}g)";
    }
}