// Singleton registry of quest applications; auto-approves until the Guild Master's Office exists.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class ApplicationBoard : MonoSingleton<ApplicationBoard>
{
    private class Application
    {
        public int Id;
        public ApplicationType Type;
        public int QuestId = -1;
        public int PartyId = -1;
        public int AdventurerId = -1;
        public ApplicationStatus Status;
    }
    
    private readonly Dictionary<int, Application> _applications = new Dictionary<int, Application>();
    
    public int SubmitQuestApplication(int questId, int partyId)
    {
        var app = new Application
        {
            Id = IdService.Instance.Next(IdService.Application),
            Type = ApplicationType.Quest,
            QuestId = questId, PartyId = partyId,
            Status = ApplicationStatus.Pending
        };
        _applications.Add(app.Id, app);
        GameEventsRelay.Instance.RaiseApplicationReceived(app.Id);
        return app.Id;
    }

    public int SubmitRankUpApplication(int adventurerId)
    {
        foreach (var existing in _applications.Values.Where(existing => existing.Type == ApplicationType.RankUp &&
                                                                        existing.AdventurerId == adventurerId &&
                                                                        existing.Status == ApplicationStatus.Pending))
            return existing.Id;

        var app = new Application
        {
            Id = IdService.Instance.Next(IdService.Application),
            Type = ApplicationType.RankUp,
            AdventurerId = adventurerId,
            Status = ApplicationStatus.Pending
        };
        _applications.Add(app.Id, app);
        GameEventsRelay.Instance.RaiseApplicationReceived(app.Id);
        return app.Id;
    }
    
    public bool Approve(int applicationId)
    {
        if (!_applications.TryGetValue(applicationId, out var app))
            return false;
        if (app.Status != ApplicationStatus.Pending)
            return false;

        app.Status = ApplicationStatus.Approved;
        
        if (app.Type == ApplicationType.Quest)
        {
            GameEventsRelay.Instance.RaiseApplicationApproved(app.Id);
        }
        else
        {
            var a = AdventurerRoster.Exists ? AdventurerRoster.Instance.Get(app.AdventurerId) : null;
            a?.TryPromote(GameConfig.Instance.Adventurer.defaultRankCap);
            GameEventsRelay.Instance.RaiseRankUpApproved(app.AdventurerId);
            GameEventsRelay.Instance.RaiseAdventurerRankedUp(app.AdventurerId);
            Remove(app.Id);
        }
        return true;
    }

    public bool Reject(int applicationId)
    {
        if (!_applications.TryGetValue(applicationId, out var app))
            return false;
        app.Status = ApplicationStatus.Rejected;
        return true;
    }
    
    public ApplicationType GetType(int applicationId)
        => _applications.TryGetValue(applicationId, out var app) ? app.Type : ApplicationType.Quest;

    public (int questId, int partyId) GetQuestTargets(int applicationId)
        => _applications.TryGetValue(applicationId, out var app) && app.Type == ApplicationType.Quest
            ? (app.QuestId, app.PartyId) : (-1, -1);
    
    public int GetAdventurerTarget(int applicationId)
        => _applications.TryGetValue(applicationId, out var app) && app.Type == ApplicationType.RankUp
            ? app.AdventurerId : -1;
    
    public IReadOnlyList<int> GetPendingIds()
        => (from kvp in _applications where kvp.Value.Status == ApplicationStatus.Pending select kvp.Key).ToList();
    
    public (int questId, int partyId) GetDetails(int applicationId)
        => GetQuestTargets(applicationId);

    public bool Remove(int applicationId)
        => _applications.Remove(applicationId);
}