// Reputation HUD view: positions the emoji/arrow on a tier-colored bar and slides the bar in/out.

using UnityEngine;
using UnityEngine.UIElements;

public class ReputationHudView
{
    private readonly VisualElement _barContainer;
    private readonly VisualElement _emoji;
    
    private static readonly Color[] TierColors =
    {
        new(0.55f, 0.10f, 0.10f),
        new(0.85f, 0.20f, 0.20f),
        new(0.95f, 0.55f, 0.15f),
        new(0.95f, 0.85f, 0.20f),
        new(0.65f, 0.85f, 0.25f),
        new(0.30f, 0.75f, 0.30f),
    };
    
    public ReputationHudView(VisualElement root)
    {
        _barContainer = root.Q<VisualElement>("rep-bar-container");
        _emoji = root.Q<VisualElement>("rep-emoji");
    }
    
    public void SetSlide(float t)
    {
        var restingPercent = 90f;
        var translate = Mathf.Lerp(restingPercent, 0f, Mathf.Clamp01(t));
        _barContainer.style.translate = new Translate(Length.Percent(translate), 0, 0);
    }
    
    public void SetEmojiPosition(float normalized)
        => _emoji.style.left = Length.Percent(Mathf.Clamp01(normalized) * 100f);
    
    public void SetTierColor(ReputationTier tier)
    {
        var i = (int)tier;
        var c = i >= 0 && i < TierColors.Length ? TierColors[i] : Color.white;
        _emoji.style.unityBackgroundImageTintColor = c;
    }

    public static Color ColorFor(ReputationTier tier)
    {
        var i = (int)tier;
        return i >= 0 && i < TierColors.Length ? TierColors[i] : Color.white;
    }
}