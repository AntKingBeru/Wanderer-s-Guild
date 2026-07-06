// Observer bridge: spawns/despawns adventurer visuals and sets movement goals from game events.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-60)]
public class AdventurerPresenceController : MonoSingleton<AdventurerPresenceController>
{
    [Header("Content")]
    [SerializeField] private GameObject adventurerPrefab;
    [SerializeField] private AdventurerClassTemplate[] classTemplates;
    
    private AdventurerVisualFactory _factory;
    private readonly Dictionary<int, AdventurerVisual> _visuals = new Dictionary<int, AdventurerVisual>();

    protected override void OnSingletonAwake() =>
        _factory = new AdventurerVisualFactory(adventurerPrefab);
    
    private void OnEnable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onAdventurerRecruited.AddListener(HandleRecruited);
        relay.onAdventurerDeparted.AddListener(HandleDeparted);
        relay.onAdventurerArrived.AddListener(HandleArrived);
        relay.onQuestStateChanged.AddListener(HandleQuestStateChanged);
    }

    private void OnDisable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onAdventurerRecruited.RemoveListener(HandleRecruited);
        relay.onAdventurerDeparted.RemoveListener(HandleDeparted);
        relay.onAdventurerArrived.RemoveListener(HandleArrived);
        relay.onQuestStateChanged.RemoveListener(HandleQuestStateChanged);
    }
    
    private void HandleRecruited(int id)
    {
        var adv = AdventurerRoster.Instance.Get(id);
        if (adv == null || _visuals.ContainsKey(id))
            return;

        var visual = _factory.Create(adv, SpriteFor(adv.Class));
        _visuals.Add(id, visual);

        ReceptionQueue.Instance.Join(id);
        visual.SetGoal(MovementGoal.ToReception);
    }
    
    private void HandleDeparted(int id, DepartureReason reason)
    {
        ReceptionQueue.Instance.Leave(id);
        if (_visuals.TryGetValue(id, out var visual) && visual)
            Destroy(visual.gameObject);
        _visuals.Remove(id);
    }
    
    private void HandleArrived(int id, MovementGoal goal)
    {
        switch (goal)
        {
            case MovementGoal.ToReception:
                ReceptionQueue.Instance.Leave(id);
                SetGoal(id, MovementGoal.Patrol);
                break;
            case MovementGoal.ToExit:
                if (_visuals.TryGetValue(id, out var v) && v)
                    v.SetVisible(false);
                break;
        }
    }
    
    private void HandleQuestStateChanged(int questId, QuestState state)
    {
        SyncGoalsFromState();
    }
    
    private void SyncGoalsFromState()
    {
        foreach (var kvp in _visuals)
        {
            var adv = AdventurerRoster.Instance.Get(kvp.Key);
            if (adv == null || !kvp.Value)
                continue;

            switch (adv.State)
            {
                case AdventurerState.Applying:
                    SetGoalIfChanged(kvp.Value, MovementGoal.ToBoard);
                    break;
                case AdventurerState.OnQuest:
                    SetGoalIfChanged(kvp.Value, MovementGoal.ToExit);
                    break;
                case AdventurerState.Idle:
                    if (kvp.Value)
                    {
                        kvp.Value.SetVisible(true);
                        RerenderVisual(kvp.Key, kvp.Value);
                    }
                    SetGoalIfChanged(kvp.Value, MovementGoal.Patrol);
                    break;
            }
        }
    }
    
    private readonly Dictionary<int, MovementGoal> _lastGoal = new Dictionary<int, MovementGoal>();

    private void SetGoalIfChanged(AdventurerVisual visual, MovementGoal goal)
    {
        if (_lastGoal.TryGetValue(visual.AdventurerId, out var last) && last == goal)
            return;
        _lastGoal[visual.AdventurerId] = goal;
        visual.SetGoal(goal);
    }
    
    private void SetGoal(int id, MovementGoal goal)
    {
        if (_visuals.TryGetValue(id, out var v) && v)
            SetGoalIfChanged(v, goal);
    }
    
    private void RerenderVisual(int id, AdventurerVisual visual)
    {
        var a = AdventurerRoster.Instance.Get(id);
        if (a == null)
            return;
        visual.Render(new BillboardInfo(a.Level, a.Name, a.Class, a.Rank), SpriteFor(a.Class));
    }
    
    private Sprite SpriteFor(AdventurerClass adventurerClass)
    {
        return classTemplates == null
            ? null
            : (from t in classTemplates
                where t && t.Class == adventurerClass select t.Sprite).FirstOrDefault();
    }
}