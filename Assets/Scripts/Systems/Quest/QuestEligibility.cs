// Pure eligibility rules matching an adventurer against a posted quest's requirements.

public static class QuestEligibility
{
    public static bool IsEligible(Adventurer adventurer, Quest quest)
    {
        if (adventurer == null || quest == null)
            return false;
        if (adventurer.State != AdventurerState.Idle)
            return false;
        if (quest.State != QuestState.Posted)
            return false;

        var rank = (int)adventurer.Rank;
        return rank >= (int)quest.Config.minRank && rank <= (int)quest.Config.maxRank;
    }
}