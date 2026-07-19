// Drives an adventurer's movement State Machine over a NavMeshAgent and reports arrivals.

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AdventurerMovement : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    private MovementStateBase _state;
    private int _adventurerId;
    private bool _arrivedFired;

    public void Initialize(int adventurerId)
    {
        _adventurerId = adventurerId;
        agent.speed = GameConfig.Instance.World.agentSpeed;
    }

    public void SetGoal(MovementGoal goal)
    {
        _state = goal switch
        {
            MovementGoal.ToReception => new ToReceptionState(agent, _adventurerId),
            MovementGoal.ToBoard => new ToBoardState(agent, _adventurerId),
            MovementGoal.ToExit => new ToExitState(agent, _adventurerId),
            MovementGoal.Patrol => new PatrolState(agent, _adventurerId),
            _ => null
        };
        _arrivedFired = false;
        _state?.Enter();
    }

    private void Update()
    {
        if (_state == null)
            return;

        if (_state is PatrolState patrol)
        {
            patrol.Tick();
            return;
        }

        if (_state is ToReceptionState reception)
        {
            reception.Retarget();
            return;
        }

        if (!_arrivedFired && _state.HasArrived())
        {
            _arrivedFired = true;
            GameEventsRelay.Instance.RaiseAdventurerArrived(_adventurerId, _state.Goal);
        }
    }
}