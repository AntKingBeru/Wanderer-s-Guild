// Pure service: assembles a party of eligible adventurers sized to a quest's party-size window.

using System.Collections.Generic;

public static class PartyFormationService
{
    public static List<int> TryFormFor(Quest quest, IReadOnlyList<Adventurer> roster)
    {
        if (quest == null || roster == null)
            return null;

        var eligible = new List<int>();
        foreach (var a in roster)
        {
            if (QuestEligibility.IsEligible(a, quest))
                eligible.Add(a.Id);
            if (eligible.Count >= quest.Config.maxPartySize)
                break;
        }

        if (eligible.Count < quest.Config.minPartySize)
            return null;
        
        var take = System.Math.Min(eligible.Count, quest.Config.maxPartySize);
        return eligible.GetRange(0, take);
    }
}
