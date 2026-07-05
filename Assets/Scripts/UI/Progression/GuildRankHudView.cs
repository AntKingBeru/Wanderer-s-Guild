// Guild-rank HUD view: a palette-colored fill bar flanked by current/next rank labels.

using UnityEngine;
using UnityEngine.UIElements;

public class GuildRankHudView
{
    private readonly VisualElement _fill;
    private readonly Label _currentRank;
    private readonly Label _nextRank;
    private readonly RankPalette _palette;
    
    public GuildRankHudView(VisualElement root, RankPalette palette)
    {
        _fill = root.Q<VisualElement>("rank-fill");
        _currentRank = root.Q<Label>("rank-current");
        _nextRank = root.Q<Label>("rank-next");
        _palette = palette;
    }
    
    public void SetFill(float ratio)
        => _fill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
    
    public void SetRanks(GuildRank current, bool atMax)
    {
        if (atMax)
        {
            _currentRank.style.display = DisplayStyle.None;
            _nextRank.style.display = DisplayStyle.None;
            _fill.style.backgroundColor = ColorForGuild(current);
            return;
        }

        _currentRank.style.display = DisplayStyle.Flex;
        _nextRank.style.display = DisplayStyle.Flex;

        var next = (GuildRank)((int)current + 1);
        _currentRank.text = current.ToString();
        _nextRank.text = next.ToString();
        _currentRank.style.color = ColorForGuild(current);
        _nextRank.style.color = ColorForGuild(next);
        _fill.style.backgroundColor = ColorForGuild(current);
    }
    
    private Color ColorForGuild(GuildRank rank) =>
        _palette ? _palette.GetColor(rank) : Color.white;
}