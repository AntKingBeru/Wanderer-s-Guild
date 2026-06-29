// Builds quest card VisualElements (list row / slot / ghost) with rank-colored borders.

using UnityEngine;
using UnityEngine.UIElements;

public class QuestCardFactory
{
    private readonly RankPalette _palette;

    public QuestCardFactory(RankPalette palette)
        => _palette = palette;
    
    public VisualElement Build(Quest quest, string extraClass = null)
    {
        var card = new VisualElement { name = $"quest-{quest.Id}" };
        card.AddToClassList("quest-card");
        if (!string.IsNullOrEmpty(extraClass)) card.AddToClassList(extraClass);

        var rankColor = _palette ? _palette.GetColor(quest.Config.minRank) : Color.white;
        SetAllBorderColors(card, rankColor);

        var title = new Label(quest.Objective);
        title.AddToClassList("quest-card__title");
        var meta = new Label($"{quest.Category}  •  {quest.Config.minRank}-{quest.Config.maxRank}  •  {quest.RewardGold}g");
        meta.AddToClassList("quest-card__meta");
        var party = new Label($"Party {quest.Config.minPartySize}-{quest.Config.maxPartySize}");
        party.AddToClassList("quest-card__meta");

        card.Add(title);
        card.Add(meta);
        card.Add(party);
        return card;
    }
    
    private static void SetAllBorderColors(VisualElement e, Color c)
    {
        e.style.borderTopColor = c; e.style.borderBottomColor = c;
        e.style.borderLeftColor = c; e.style.borderRightColor = c;
    }
}