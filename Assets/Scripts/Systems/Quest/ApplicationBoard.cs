// Singleton registry of quest applications; auto-approves until the Guild Master's Office exists.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class ApplicationBoard : MonoSingleton<ApplicationBoard>
{
    private class Application
    {
        public int Id;
        public int QuestId;
        public int PartyId;
        public ApplicationStatus Status;
    }
    
    private readonly Dictionary<int, Application> _applications = new Dictionary<int, Application>();
    
    public int Submit(int questId, int partyId)
    {
        var app = new Application
        {
            Id = IdService.Instance.Next(IdService.Application),
            QuestId = questId,
            PartyId = partyId,
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
        GameEventsRelay.Instance.RaiseApplicationApproved(app.Id);
        return true;
    }

    public bool Reject(int applicationId)
    {
        if (!_applications.TryGetValue(applicationId, out var app))
            return false;
        app.Status = ApplicationStatus.Rejected;
        return true;
    }

    public (int questId, int partyId) GetTargets(int applicationId)
        => _applications.TryGetValue(applicationId, out var app) ? (app.QuestId, app.PartyId) : (-1, -1);

    public bool Remove(int applicationId)
        => _applications.Remove(applicationId);
    
    private static bool RequiresManualApproval()
        => FacilityController.Exists && FacilityController.Instance.IsBuilt(FacilityType.Office);
}