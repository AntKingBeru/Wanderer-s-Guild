// Assembles a display model (quest text + party member lines) for an application popup, read live.

using System.Collections.Generic;

public class ApplicationDetailAssembler
{
    public static string BuildHeader(int applicationId)
    {
        if (!ApplicationBoard.Exists)
            return "Application";
        var type = ApplicationBoard.Instance.GetType(applicationId);

        if (type == ApplicationType.RankUp)
        {
            var advId = ApplicationBoard.Instance.GetAdventurerTarget(applicationId);
            var a = AdventurerRoster.Exists ? AdventurerRoster.Instance.Get(advId) : null;
            return a != null ? $"Rank-Up Request — {a.Name}" : "Rank-Up Request";
        }

        var (questId, _) = ApplicationBoard.Instance.GetQuestTargets(applicationId);
        var quest = FindQuest(questId);
        return quest != null ? $"Quest Application — {quest.Objective}" : "Quest Application";
    }
    
    public static string BuildSubheader(int applicationId)
    {
        if (!ApplicationBoard.Exists)
            return string.Empty;
        var type = ApplicationBoard.Instance.GetType(applicationId);

        if (type == ApplicationType.RankUp)
        {
            var advId = ApplicationBoard.Instance.GetAdventurerTarget(applicationId);
            var a = AdventurerRoster.Exists ? AdventurerRoster.Instance.Get(advId) : null;
            if (a == null)
                return string.Empty;
            var next = a.Rank < GuildRank.S ? (GuildRank)((int)a.Rank + 1) : a.Rank;
            return $"{a.Class}  •  Rank {a.Rank} → {next}  •  Level {a.Level}";
        }

        var (questId, _) = ApplicationBoard.Instance.GetQuestTargets(applicationId);
        var quest = FindQuest(questId);
        return quest != null
            ? $"{quest.Category}  •  Required Rank {quest.Config.requiredRank}  •  {quest.RewardGold}g"
            : string.Empty;
    }
    
    public static List<MemberLine> BuildMembers(int applicationId)
    {
        var lines = new List<MemberLine>();
        if (!ApplicationBoard.Exists || !AdventurerRoster.Exists)
            return lines;

        var kind = ApplicationBoard.Instance.GetType(applicationId);

        if (kind == ApplicationType.RankUp)
        {
            var advId = ApplicationBoard.Instance.GetAdventurerTarget(applicationId);
            var a = AdventurerRoster.Instance.Get(advId);
            if (a != null)
                lines.Add(new MemberLine(a.Name, a.Class, a.Rank, a.Level, true));
            return lines;
        }

        var (_, partyId) = ApplicationBoard.Instance.GetQuestTargets(applicationId);
        var members = QuestLifecycleController.Exists
            ? QuestLifecycleController.Instance.GetPartyMembers(partyId)
            : System.Array.Empty<int>();
        
        for (var i = 0; i < members.Count; i++)
        {
            var a = AdventurerRoster.Instance.Get(members[i]);
            if (a != null)
                lines.Add(new MemberLine(a.Name, a.Class, a.Rank, a.Level, i == 0));
        }
        return lines;
    }
    
    private static Quest FindQuest(int questId)
    {
        if (!QuestBoard.Exists)
            return null;
        for (var i = 0; i < QuestBoard.Instance.SlotCount; i++)
        {
            var quest = QuestBoard.Instance.GetSlot(i);
            if (quest != null && quest.Id == questId)
                return quest;
        }
        return null;
    }
}