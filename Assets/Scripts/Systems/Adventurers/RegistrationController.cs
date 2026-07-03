// Gates new-adventurer registration: auto-registers without an office, else holds pending with a grace period.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-64)]
public class RegistrationController : MonoSingleton<RegistrationController>
{
    private class Pending { public int AdventurerId; public GameDate ExpiresOn; }
    
    private readonly Dictionary<int, Pending> _pending = new Dictionary<int, Pending>();
    
    public IReadOnlyCollection<int> PendingIds => _pending.Keys;

    private void OnEnable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onAdventurerRecruited.AddListener(HandleRecruited);
        relay.onDayAdvanced.AddListener(HandleDayAdvanced);
    }
    
    private void OnDisable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onAdventurerRecruited.RemoveListener(HandleRecruited);
        relay.onDayAdvanced.RemoveListener(HandleDayAdvanced);
    }
    
    public bool IsPending(int adventurerId)
        => _pending.ContainsKey(adventurerId);
    
    private void HandleRecruited(int adventurerId)
    {
        if (!OfficeBuilt())
        {
            GameEventsRelay.Instance.RaiseRegistrationApproved(adventurerId);
            return;
        }

        var config = GameConfig.Instance.Adventurer;
        var expires = TimeController.Instance.CurrentDate.AddDays(
            System.Math.Max(1, config.registrationGraceDays), GameConfig.Instance.Time.daysPerSeason);

        _pending[adventurerId] = new Pending { AdventurerId = adventurerId, ExpiresOn = expires };
        AdventurerRoster.Instance.Get(adventurerId)?.SetState(AdventurerState.Applying);
        GameEventsRelay.Instance.RaiseRegistrationPending(adventurerId);
    }
    
    public bool Approve(int adventurerId)
    {
        if (!_pending.Remove(adventurerId))
            return false;
        AdventurerRoster.Instance.Get(adventurerId)?.SetState(AdventurerState.Idle);
        GameEventsRelay.Instance.RaiseRegistrationApproved(adventurerId);
        return true;
    }
    
    public bool Reject(int adventurerId)
    {
        if (!_pending.Remove(adventurerId))
            return false;
        AdventurerRoster.Instance.Remove(adventurerId, DepartureReason.Dismissed);
        return true;
    }
    
    private void HandleDayAdvanced(GameDate today)
    {
        var expired = (from kvp in _pending where today.CompareTo(kvp.Value.ExpiresOn) >= 0 select kvp.Key).ToList();

        foreach (var id in expired)
        {
            _pending.Remove(id);
            AdventurerRoster.Instance.Remove(id, DepartureReason.NoOpportunities);
        }
    }
    
    private static bool OfficeBuilt()
        => FacilityController.Exists && FacilityController.Instance.IsBuilt(FacilityType.Office);
}