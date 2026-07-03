// Central Mediator (Singleton) broadcasting decoupled UnityEvents for every system.

using System;
using UnityEngine;
using UnityEngine.Events;

// Boots right after GameConfig, before gameplay systems
[DefaultExecutionOrder(-100)]
public class GameEventsRelay : MonoSingleton<GameEventsRelay>
{
    #region Event Type Definitions
    [Serializable] public class GuildRankEvent : UnityEvent<GuildRank> { }
    [Serializable] public class ReputationEvent : UnityEvent<int, ReputationChangeReason> { }
    [Serializable] public class QuestStateEvent : UnityEvent<int, QuestState> { }
    [Serializable] public class QuestResultEvent : UnityEvent<int, bool> { }
    [Serializable] public class QuestOutcomeEvent : UnityEvent<int, QuestOutcome> { }
    [Serializable] public class IntEvent : UnityEvent<int> { }
    [Serializable] public class GoldEvent : UnityEvent<int, int> { }
    [Serializable] public class TransactionEvent : UnityEvent<Transaction> { }
    [Serializable] public class FacilityEvent : UnityEvent<FacilityType> { }
    [Serializable] public class DepartureEvent : UnityEvent<int, DepartureReason> { }
    [Serializable] public class TimeSpeedEvent : UnityEvent<TimeSpeed> { }
    [Serializable] public class DateEvent : UnityEvent<GameDate> { }
    [Serializable] public class SeasonEvent : UnityEvent<Season> { }
    [Serializable] public class ScreenEvent : UnityEvent<ScreenId> { }
    [Serializable] public class MovementArrivedEvent : UnityEvent<int, MovementGoal> { }
    #endregion

    #region Guild Rank System
    public GuildRankEvent onGuildRankChanged = new();

    public void RaiseGuildRankChanged(GuildRank rank)
        => onGuildRankChanged?.Invoke(rank);
    #endregion

    #region Reputation System
    public ReputationEvent onReputationChanged = new();

    public void RaiseReputationChanged(int delta, ReputationChangeReason reason)
        => onReputationChanged?.Invoke(delta, reason);
    #endregion

    #region Quest System
    public IntEvent onRequestGenerated = new();
    public IntEvent onRequestExpired = new();
    public IntEvent onQuestPosted = new();
    public IntEvent onApplicationReceived = new();
    public IntEvent onQuestExpired = new();
    public QuestStateEvent onQuestStateChanged = new();
    public QuestResultEvent onQuestResolved = new();
    public IntEvent onQuestCreated = new();

    public void RaiseRequestGenerated(int requestId)
        => onRequestGenerated?.Invoke(requestId);

    public void RaiseRequestExpired(int questId)
        => onRequestExpired?.Invoke(questId);

    public void RaiseQuestPosted(int questId)
        => onQuestPosted?.Invoke(questId);

    public void RaiseApplicationReceived(int appId)
        => onApplicationReceived?.Invoke(appId);

    public void RaiseQuestExpired(int questId)
        => onQuestExpired?.Invoke(questId);

    public void RaiseQuestStateChanged(int questId, QuestState state)
        => onQuestStateChanged?.Invoke(questId, state);

    public void RaiseQuestResolved(int questId, bool success)
        => onQuestResolved?.Invoke(questId, success);

    public void RaiseQuestCreated(int questId)
        => onQuestCreated?.Invoke(questId);
    #endregion

    #region Adventurer System
    public IntEvent onAdventurerRecruited = new();
    public IntEvent onAdventurerLeveledUp = new();
    public IntEvent onAdventurerRankedUp = new();
    public DepartureEvent onAdventurerDeparted = new();

    public void RaiseAdventurerRecruited(int id)
        => onAdventurerRecruited?.Invoke(id);

    public void RaiseAdventurerLeveledUp(int id)
        => onAdventurerLeveledUp?.Invoke(id);

    public void RaiseAdventurerRankedUp(int id)
        => onAdventurerRankedUp?.Invoke(id);

    public void RaiseAdventurerDeparted(int id, DepartureReason reason)
        => onAdventurerDeparted?.Invoke(id, reason);
    #endregion

    #region Party System
    public IntEvent onPartyFormed = new();
    public IntEvent onPartyDisbanded = new();
    public IntEvent onApplicationApproved = new();
    public QuestOutcomeEvent onQuestOutcome = new();

    public void RaisePartyFormed(int id)
        => onPartyFormed?.Invoke(id);

    public void RaisePartyDisbanded(int id)
        => onPartyDisbanded?.Invoke(id);

    public void RaiseApplicationApproved(int applicationId)
        => onApplicationApproved?.Invoke(applicationId);

    public void RaiseQuestOutcome(int questId, QuestOutcome outcome)
        => onQuestOutcome?.Invoke(questId, outcome);
    #endregion

    #region Facility System
    public FacilityEvent onFacilityBuilt = new();
    public FacilityEvent onFacilityUpgraded = new();

    public void RaiseFacilityBuilt(FacilityType type)
        => onFacilityBuilt?.Invoke(type);

    public void RaiseFacilityUpgraded(FacilityType type)
        => onFacilityUpgraded?.Invoke(type);
    #endregion

    #region Economy System
    public GoldEvent onGoldChanged = new();
    public TransactionEvent onTransaction = new();

    public void RaiseGoldChanged(int newTotal, int delta)
        => onGoldChanged?.Invoke(newTotal, delta);

    public void RaiseTransaction(Transaction tx)
        => onTransaction?.Invoke(tx);
    #endregion

    #region Time & Simulation System
    public TimeSpeedEvent onTimeSpeedChanged = new();
    public DateEvent onDayAdvanced = new();
    public SeasonEvent onSeasonChanged = new();

    public void RaiseTimeSpeedChanged(TimeSpeed speed)
        => onTimeSpeedChanged?.Invoke(speed);

    public void RaiseDayAdvanced(GameDate date)
        => onDayAdvanced?.Invoke(date);

    public void RaiseSeasonChanged(Season season)
        => onSeasonChanged?.Invoke(season);
    #endregion

    #region UI
    public ScreenEvent onScreenOpened = new();
    public ScreenEvent onScreenClosed = new();

    public void RaiseScreenOpened(ScreenId id)
        => onScreenOpened?.Invoke(id);

    public void RaiseScreenClosed(ScreenId id)
        => onScreenClosed?.Invoke(id);
    #endregion
    
    #region In-World
    public MovementArrivedEvent onAdventurerArrived = new();
    
    public void RaiseAdventurerArrived(int id, MovementGoal goal)
        => onAdventurerArrived?.Invoke(id, goal);
    #endregion
}