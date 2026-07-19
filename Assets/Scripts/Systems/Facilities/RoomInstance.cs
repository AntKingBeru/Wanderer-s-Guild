// Presents one-placed room: swaps staged prefabs during construction and reveals level add-ons.

using System.Collections.Generic;
using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    private FacilityType _type;
    private FacilityData _data;
    private GameObject _currentStageObject;
    private readonly List<GameObject> _addOns = new List<GameObject>();
    private ConstructionStage _stage = ConstructionStage.Empty;
    
    public void Initialize(FacilityType type, FacilityData data)
    {
        _type = type;
        _data = data;
        SetStage(ConstructionStage.Empty);
    }
    
    public void InitializeAsFinished(FacilityType type, FacilityData data)
    {
        _type = type;
        _data = data;
        _stage = ConstructionStage.Finished;
    }
    
    private void OnEnable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onFacilityConstructionStarted.AddListener(HandleConstructionStarted);
        relay.onFacilityBuilt.AddListener(HandleBuilt);
        relay.onFacilityUpgraded.AddListener(HandleUpgraded);
    }

    private void OnDisable()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onFacilityConstructionStarted.RemoveListener(HandleConstructionStarted);
        relay.onFacilityBuilt.RemoveListener(HandleBuilt);
        relay.onFacilityUpgraded.RemoveListener(HandleUpgraded);
    }

    private void Update()
    {
        if (_stage == ConstructionStage.Finished)
            return;
        if (!ConstructionController.Exists || !ConstructionController.Instance.IsUnderConstruction(_type))
            return;

        var progress = ConstructionController.Instance.GetProgress(_type);
        var want =
            progress >= 1f ? ConstructionStage.Finished :
            progress >= 0.5f ? ConstructionStage.LateScaffolding :
            progress > 0f ? ConstructionStage.EarlyScaffolding : ConstructionStage.Empty;

        if (want != _stage)
            SetStage(want);
    }

    private void HandleConstructionStarted(FacilityType type)
    {
        if (type == _type)
            SetStage(ConstructionStage.EarlyScaffolding);
    }

    private void HandleBuilt(FacilityType type)
    {
        if (type == _type)
            SetStage(ConstructionStage.Finished);
    }
    
    private void HandleUpgraded(FacilityType type)
    {
        if (type != _type || !FacilityController.Exists)
            return;
        var level = FacilityController.Instance.GetLevel(_type);
        var addOnPrefab = _data.AddOnForLevel(level);
        if (addOnPrefab)
            _addOns.Add(Instantiate(addOnPrefab, transform.position, transform.rotation, transform));
    }
    
    private void SetStage(ConstructionStage stage)
    {
        _stage = stage;
        if (_currentStageObject)
            Destroy(_currentStageObject);

        var prefab = _data.StagePrefab(stage);
        _currentStageObject = prefab
            ? Instantiate(prefab, transform.position, transform.rotation, transform)
            : null;
    }
}