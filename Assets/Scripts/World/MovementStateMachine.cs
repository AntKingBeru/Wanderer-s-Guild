// Movement states for an in-world adventurer: each supplies a NavMesh destination and arrival check.

using UnityEngine;
using UnityEngine.AI;

// Base state: owns shared context and the arrival test; subclasses pick the destination.
public abstract class MovementStateBase
{
    protected readonly NavMeshAgent agent;
    protected readonly int adventurerId;

    protected MovementStateBase(NavMeshAgent agent, int adventurerId)
    {
        this.agent = agent;
        this.adventurerId = adventurerId;
    }
    
    public abstract MovementGoal Goal { get; }

    public abstract void Enter();

    public virtual bool HasArrived()
    {
        if (agent.pathPending)
            return false;
        return agent.remainingDistance <= GameConfig.Instance.World.arrivalTolerance;
    }
}


// Walks to (and continually re-targets) this adventurer's reception-queue slot.
public class ToReceptionState : MovementStateBase
{
    public ToReceptionState(NavMeshAgent agent, int id) : base(agent, id) { }
    public override MovementGoal Goal => MovementGoal.ToReception;

    public override void Enter()
        => Retarget();

    public void Retarget()
    {
        var idx = ReceptionQueue.Instance.IndexOf(adventurerId);
        if (idx < 0)
            idx = ReceptionQueue.Instance.Join(adventurerId);
        agent.SetDestination(ReceptionQueue.Instance.SlotPosition(idx));
    }
}

// Walks to the guild board anchor.
public class ToBoardState : MovementStateBase
{
    public ToBoardState(NavMeshAgent agent, int id) : base(agent, id)
    {
    }

    public override MovementGoal Goal => MovementGoal.ToBoard;

    public override void Enter()
    {
        var transform = WorldAnchors.Instance.BoardPoint;
        if (transform)
            agent.SetDestination(transform.position);
    }
}

// Walks to the guild exit anchor (after which the presence controller despawns the visual).
public class ToExitState : MovementStateBase
{
    public ToExitState(NavMeshAgent agent, int id) : base(agent, id) { }
    public override MovementGoal Goal => MovementGoal.ToExit;
    
    public override void Enter()
    {
        var transform = WorldAnchors.Instance.ExitPoint;
        if (transform)
            agent.SetDestination(transform.position);
    }
}

// Picks random reachable points around the guild and wanders between them.
public class PatrolState : MovementStateBase
{
    private float _waitUntil;

    public PatrolState(NavMeshAgent agent, int id) : base(agent, id) { }
    public override MovementGoal Goal => MovementGoal.Patrol;

    public override void Enter()
        => PickNewPoint();

    // NOTE: patrol never "arrives"; the runner calls Tick to pick a new point after a pause.
    public override bool HasArrived()
        => false;

    public void Tick()
    {
        if (agent.pathPending)
            return;
        if (agent.remainingDistance > GameConfig.Instance.World.arrivalTolerance)
            return;
        if (Time.time < _waitUntil)
            return;
        PickNewPoint();
    }

    private void PickNewPoint()
    {
        var config = GameConfig.Instance.World;
        var random = agent.transform.position + Random.insideUnitSphere * config.patrolRadius;
        if (NavMesh.SamplePosition(random, out var hit, config.patrolRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        _waitUntil = Time.time + Random.Range(config.minPatrolWaitSeconds, config.maxPatrolWaitSeconds);
    }
}