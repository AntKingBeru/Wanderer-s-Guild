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
        if (!string.IsNullOrEmpty(extraClass))
            card.AddToClassList(extraClass);

        var rankColor = ColorFor(quest);
        SetAllBorderColors(card, rankColor);

        card.Add(MakeLabel(quest.Objective, "quest-card__title"));
        card.Add(MakeLabel($"{quest.Category}  •  Rank {quest.Config.requiredRank}  •  {quest.RewardGold}g", "quest-card__meta"));
        card.Add(MakeLabel($"Party {quest.Config.minPartySize}-{quest.Config.maxPartySize}", "quest-card__meta"));
        return card;
    }
    
    public VisualElement BuildPaper(Quest quest)
    {
        var paper = new VisualElement { name = $"quest-paper-{quest.Id}" };
        paper.AddToClassList("quest-paper");

        var rankColor = ColorFor(quest);
        
        paper.Add(MakeCorner("quest-paper__corner--tl", rankColor));
        paper.Add(MakeCorner("quest-paper__corner--tr", rankColor));
        paper.Add(MakeCorner("quest-paper__corner--bl", rankColor));
        paper.Add(MakeCorner("quest-paper__corner--br", rankColor));
        
        var content = new VisualElement();
        content.AddToClassList("quest-paper__content");
        content.Add(MakeLabel(quest.Objective, "quest-paper__title"));
        content.Add(MakeLabel($"{quest.Category}", "quest-paper__sub"));
        content.Add(MakeLabel($"Rank {quest.Config.requiredRank}", "quest-paper__meta"));
        content.Add(MakeLabel($"Party {quest.Config.minPartySize}-{quest.Config.maxPartySize}", "quest-paper__meta"));
        content.Add(MakeLabel($"{quest.RewardGold}g", "quest-paper__reward"));
        paper.Add(content);

        return paper;
    }
    
    private static VisualElement MakeCorner(string cornerClass, Color color)
    {
        var corner = new VisualElement();
        corner.AddToClassList("quest-paper__corner");
        corner.AddToClassList(cornerClass);
        corner.style.backgroundColor = color;
        return corner;
    }
    
    private static Label MakeLabel(string text, string cls)
    {
        var label = new Label(text);
        label.AddToClassList(cls);
        return label;
    }
    
    private Color ColorFor(Quest quest) =>
        _palette ? _palette.GetColor(quest.Config.requiredRank) : Color.white;
    
    private static void SetAllBorderColors(VisualElement e, Color c)
    {
        e.style.borderTopColor = c; e.style.borderBottomColor = c;
        e.style.borderLeftColor = c; e.style.borderRightColor = c;
    }
}