// Drives the in-world movement of a single adventurer using Unity's NavMeshAgent.
// Implements a State Machine pattern: each AdventurerBehaviourState maps to a
// distinct enter/tick/exit behavior.
// Listens to GameEventRelay events to know when to transition states
// (e.g., a quest dispatched → Departing, a quest resolved → Returning).
// AdventurerWorldManager owns this component and calls Initialize() after spawn.

using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AdventurerNavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    
    [Header("Patrol Area")]
    [Tooltip("Centre of the area this adventurer wanders when idle.")]
    [SerializeField] private Transform patrolCenter;

    [Tooltip("Radius around patrolCenter within which random wander targets are chosen.")]
    [SerializeField, Min(0.5f)] private float patrolRadius = 5f;

    [Tooltip("How long (real seconds) the adventurer idles at a wander point before picking the next one.")]
    [SerializeField, Min(0.5f)] private float wanderWaitTime = 3f;

    [Header("Arrival Threshold")]
    [Tooltip("Distance (world units) at which the agent is considered to have reached its destination.")]
    [SerializeField, Min(0.1f)] private float arrivalDistance = 0.5f;
    
    private AdventurerData _adventurer;

    private AdventurerBehaviorState _state = AdventurerBehaviorState.Idle;

    // Timer used when idling at a wander point before picking the next one.
    private float _wanderTimer;

    // Whether the agent has reached its current destination this tick.
    private bool _destinationReached;
    
    #region Lifecycle
    // Called by AdventurerWorldManager right after Instantiate + Initialize().
    public void InitializeNavigation(AdventurerData adventurer, Transform patrolAreaCenter)
    {
        _adventurer   = adventurer;
        patrolCenter  = patrolAreaCenter;

        // Subscribe to relay events that drive state transitions.
        if (GameEventRelay.Instance)
        {
            GameEventRelay.Instance.OnQuestStatusChanged.AddListener(HandleQuestStatusChanged);
            GameEventRelay.Instance.OnRankUpApplicationResolved.AddListener(HandleRankUpResolved);
            GameEventRelay.Instance.OnAdventurerRankUp.AddListener(HandleRankUpFinished);
            GameEventRelay.Instance.OnAdventurerRankUpFailed.AddListener(HandleRankUpFinished);
        }

        // First action after spawn: walk to a reception desk.
        EnterState(AdventurerBehaviorState.Arriving);
    }

    private void OnDestroy()
    {
        if (GameEventRelay.Instance)
        {
            GameEventRelay.Instance.OnQuestStatusChanged.RemoveListener(HandleQuestStatusChanged);
            GameEventRelay.Instance.OnRankUpApplicationResolved.RemoveListener(HandleRankUpResolved);
            GameEventRelay.Instance.OnAdventurerRankUp.RemoveListener(HandleRankUpFinished);
            GameEventRelay.Instance.OnAdventurerRankUpFailed.RemoveListener(HandleRankUpFinished);
        }
    }
    
    private void Update()
    {
        _destinationReached = !agent.pathPending
                              && agent.remainingDistance <= arrivalDistance;
        switch (_state)
        {
            case AdventurerBehaviorState.Idle:
                TickIdle();
                break;
            case AdventurerBehaviorState.Arriving:
                TickArriving(); 
                break;
            case AdventurerBehaviorState.Browsing:
                TickBrowsing();
                break;
            case AdventurerBehaviorState.Departing:
                TickDeparting();
                break;
            case AdventurerBehaviorState.Returning:
                TickReturning();
                break;
            case AdventurerBehaviorState.OnQuest:
            default:
                break;
        }
    }
    #endregion
    
    #region State Machine Enter
    private void EnterState(AdventurerBehaviorState newState)
    {
        _state = newState;
        switch (newState)
        {
            case AdventurerBehaviorState.Idle:
                // Pick a random wander point immediately.
                _wanderTimer = 0f;
                MoveToRandomPatrolPoint();
                break;

            case AdventurerBehaviorState.Arriving:
                // Walk to a random reception desk.
                var desk = GuildPointRegistry.Instance?.GetRandomPoint(GuildPointType.ReceptionDesk);
                MoveTo(desk);
                break;

            case AdventurerBehaviorState.Browsing:
                // Walk to a random guild board.
                var board = GuildPointRegistry.Instance?.GetRandomPoint(GuildPointType.QuestBoard);
                MoveTo(board);
                break;

            case AdventurerBehaviorState.Departing:
                // Walk to the exit point; we'll hide once arrived.
                var exit = GuildPointRegistry.Instance?.GetRandomPoint(GuildPointType.Exit);
                MoveTo(exit);
                break;

            case AdventurerBehaviorState.OnQuest:
                // Hide the object and stop the agent.
                agent.ResetPath();
                gameObject.SetActive(false);
                break;

            case AdventurerBehaviorState.Returning:
                // Re-enable, place at exit, walk back to patrol area.
                gameObject.SetActive(true);
                var returnExit = GuildPointRegistry.Instance?.GetRandomPoint(GuildPointType.Exit);
                if (returnExit) transform.position = returnExit.position;
                MoveToRandomPatrolPoint();
                break;
        }
    }
    #endregion
    
    #region State Machine Tick
    private void TickIdle()
    {
        if (!_destinationReached)
            return;

        // Wait at the current wander point, then pick the next one.
        _wanderTimer += Time.deltaTime;
        if (_wanderTimer >= wanderWaitTime)
        {
            _wanderTimer = 0f;
            MoveToRandomPatrolPoint();
        }
    }
    
    private void TickArriving()
    {
        // Once the adventurer reaches the reception desk, transition to idle wandering.
        if (_destinationReached)
            EnterState(AdventurerBehaviorState.Idle);
    }
    
    private void TickBrowsing()
    {
        // Once the adventurer reaches the board, go back to idle.
        // The actual application was already submitted by SoloAdventurerManager;
        // this is purely the visual walk.
        if (_destinationReached)
            EnterState(AdventurerBehaviorState.Idle);
    }
    
    private void TickDeparting()
    {
        // Once at the exit, switch to OnQuest (hide).
        if (_destinationReached)
            EnterState(AdventurerBehaviorState.OnQuest);
    }
    
    private void TickReturning()
    {
        // Once back in the patrol area, resume normal wandering.
        if (_destinationReached)
            EnterState(AdventurerBehaviorState.Idle);
    }
    #endregion
    
    #region Event Handlers
    private void HandleQuestStatusChanged(QuestData quest)
    {
        if (_adventurer == null)
            return;

        switch (quest.Status)
        {
            case QuestStatus.InProgress:
                // Only depart if this adventurer is on this quest.
                if (IsOnQuest(quest))
                    EnterState(AdventurerBehaviorState.Departing);
                break;

            case QuestStatus.Completed:
            case QuestStatus.Failed:
                // Return from quest if this adventurer was on it.
                if (WasOnQuest(quest))
                    EnterState(AdventurerBehaviorState.Returning);
                break;
        }
    }
    
    private void HandleRankUpResolved(RankUpApplicationData application)
    {
        if (_adventurer == null) return;
        if (application.AdventurerId != _adventurer.Id) return;

        switch (application.Status)
        {
            case ApplicationStatus.Approved:
                // Adventurer was dispatched on their rank-up quest — head for the exit.
                EnterState(AdventurerBehaviorState.Departing);
                break;
            case ApplicationStatus.Rejected:
                // Rejected by the player — no movement change needed, stay idle.
                break;
        }
    }
    
    private void HandleRankUpFinished(AdventurerData adventurer)
    {
        if (_adventurer == null || adventurer.Id != _adventurer.Id)
            return;
        // Rank-up quest resolved — reappear at the exit and walk back in.
        EnterState(AdventurerBehaviorState.Returning);
    }
    #endregion
    
    // Public Trigger - called by AdventurerWorldObject when SoloAdventurerManager submits an application so the adventurer visually walks to the board
    public void TriggerBrowse()
    {
        // Only walk to the board if currently idle; don't interrupt other states.
        if (_state == AdventurerBehaviorState.Idle)
            EnterState(AdventurerBehaviorState.Browsing);
    }
    
    #region NavMesh Helpers
    private void MoveTo(Transform target)
    {
        if (!target || !agent.isOnNavMesh)
            return;
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }
    
    private void MoveToRandomPatrolPoint()
    {
        if (!agent.isOnNavMesh)
            return;

        // Sample a random point within patrolRadius, snapped to the NavMesh.
        var center = patrolCenter ? patrolCenter.position : transform.position;
        var randomOffset = Random.insideUnitSphere * patrolRadius;
        randomOffset.y = 0f;
        var candidate = center + randomOffset;

        if (NavMesh.SamplePosition(candidate, out var hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }
    #endregion
    
    #region Quest Membership Helpers
    // True if this adventurer's status is AppliedToQuest or OnQuest for this specific quest.
    private bool IsOnQuest(QuestData quest) 
        => quest.ApprovedApplication != null
           && quest.ApprovedApplication.PartyMemberIds.Any(id => id == _adventurer.Id);

    // True if this adventurer was a member of the quest (approved application contains their ID).
    private bool WasOnQuest(QuestData quest)
        => IsOnQuest(quest); // Same check; separate method for readability.
    #endregion
}